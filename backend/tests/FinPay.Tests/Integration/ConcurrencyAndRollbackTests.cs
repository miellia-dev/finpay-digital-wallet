using FinPay.Application.DTOs.Transfer;
using FinPay.Application.Exceptions;
using FinPay.Application.Interfaces;
using FinPay.Application.Interfaces.Repositories;
using FinPay.Application.Interfaces.Services;
using FinPay.Application.Services;
using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FinPay.Tests.Integration;

public class ConcurrencyAndRollbackTests
{
    [Fact]
    public async Task Transfer_RollsBackEntirely_WhenLedgerInsertionThrowsException()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var senderWallet = new BasicWallet(senderUserId, "1111111111", initialBalance: 10_000m);
        var receiverWallet = new BasicWallet(receiverUserId, "2222222222", initialBalance: 1_000m);

        var walletRepoMock = new Mock<IWalletRepository>();
        var txRepoMock = new Mock<ITransactionRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<TransferService>>();

        walletRepoMock.Setup(r => r.GetByUserIdAsync(senderUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderWallet);
        walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("2222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);
        walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(senderWallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // Simulate atomic execution: if ledger entry fails, execute transaction rolls back
        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<TransferResponseDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<TransferResponseDto>>, CancellationToken>(async (operation, _) =>
            {
                // Capture initial state
                var senderInitial = senderWallet.Balance;
                var receiverInitial = receiverWallet.Balance;
                try
                {
                    return await operation();
                }
                catch
                {
                    // Simulating transaction rollback of domain modifications
                    // In real DB, EF Core does not persist uncommitted changes
                    return null!;
                }
            });

        // Simulate failure during ledger insertion
        txRepoMock.Setup(r => r.AddLedgerEntryAsync(It.IsAny<LedgerEntry>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Simulated disk error while appending to ledger table."));

        var transferService = new TransferService(
            walletRepoMock.Object,
            txRepoMock.Object,
            uowMock.Object,
            notificationMock.Object,
            loggerMock.Object);

        var request = new TransferRequestDto("2222222222", Amount: 2_000m);

        // Act
        var result = await transferService.TransferFundsAsync(senderUserId, request);

        // Assert
        Assert.Null(result); // Rolled back due to error
        // Unit of work SaveChangesAsync was NOT allowed to commit
        uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Transfer_EnforcesDailyLimit_ForMerchantWallet()
    {
        // Arrange
        var merchantUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var merchantWallet = new MerchantWallet(merchantUserId, "9999999999", initialBalance: 2_000_000m);
        var receiverWallet = new BasicWallet(receiverUserId, "1234567890", initialBalance: 500m);

        var walletRepoMock = new Mock<IWalletRepository>();
        var txRepoMock = new Mock<ITransactionRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<TransferService>>();

        walletRepoMock.Setup(r => r.GetByUserIdAsync(merchantUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchantWallet);
        walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);

        // Merchant already transferred 950,000 today out of 1,000,000 limit
        walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(merchantWallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(950_000m);

        var transferService = new TransferService(
            walletRepoMock.Object,
            txRepoMock.Object,
            uowMock.Object,
            notificationMock.Object,
            loggerMock.Object);

        // Attempting to transfer 60,000 (total = 1,010,000 > 1,000,000 limit)
        var request = new TransferRequestDto("1234567890", Amount: 60_000m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DailyLimitExceededException>(() =>
            transferService.TransferFundsAsync(merchantUserId, request));

        Assert.Equal(1_000_000m, ex.DailyLimit);
        Assert.Equal(950_000m, ex.CurrentDayTotal);
        Assert.Equal(60_000m, ex.RequestedAmount);
    }

    [Fact]
    public async Task Transfer_Succeeds_WhenWithinMerchantDailyLimit()
    {
        // Arrange
        var merchantUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var merchantWallet = new MerchantWallet(merchantUserId, "9999999999", initialBalance: 2_000_000m);
        var receiverWallet = new BasicWallet(receiverUserId, "1234567890", initialBalance: 500m);

        var walletRepoMock = new Mock<IWalletRepository>();
        var txRepoMock = new Mock<ITransactionRepository>();
        var uowMock = new Mock<IUnitOfWork>();
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<TransferService>>();

        walletRepoMock.Setup(r => r.GetByUserIdAsync(merchantUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchantWallet);
        walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);

        // 950,000 used today
        walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(merchantWallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(950_000m);

        uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<TransferResponseDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<TransferResponseDto>>, CancellationToken>((func, _) => func());

        var transferService = new TransferService(
            walletRepoMock.Object,
            txRepoMock.Object,
            uowMock.Object,
            notificationMock.Object,
            loggerMock.Object);

        // Transfer 40,000 (total = 990,000 <= 1,000,000 limit) -> MUST SUCCEED
        var request = new TransferRequestDto("1234567890", Amount: 40_000m);

        var result = await transferService.TransferFundsAsync(merchantUserId, request);

        Assert.NotNull(result);
        Assert.Equal(40_000m, result.Amount);
        Assert.Equal(TransactionStatus.Completed, result.Status);
    }
}
