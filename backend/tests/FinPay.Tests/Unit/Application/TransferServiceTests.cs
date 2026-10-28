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

namespace FinPay.Tests.Unit.Application;

public class TransferServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<ITransactionRepository> _txRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly Mock<ILogger<TransferService>> _loggerMock;
    private readonly TransferService _transferService;

    public TransferServiceTests()
    {
        _walletRepoMock = new Mock<IWalletRepository>();
        _txRepoMock = new Mock<ITransactionRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _notificationMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<TransferService>>();

        // Setup UnitOfWork to execute the passed func directly
        _uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<TransferResponseDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<TransferResponseDto>>, CancellationToken>((func, _) => func());

        _transferService = new TransferService(
            _walletRepoMock.Object,
            _txRepoMock.Object,
            _uowMock.Object,
            _notificationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Transfer_Test1_ThrowsInsufficientFundsException_WhenSenderHasInsufficientBalance()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var senderWallet = new BasicWallet(senderUserId, "1111111111", initialBalance: 1_000m);
        var receiverWallet = new BasicWallet(receiverUserId, "2222222222", initialBalance: 500m);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(senderUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderWallet);
        _walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("2222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);
        _walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(senderWallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var request = new TransferRequestDto("2222222222", Amount: 2_500m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InsufficientFundsException>(() =>
            _transferService.TransferFundsAsync(senderUserId, request));

        Assert.Equal(1_000m, ex.AvailableBalance);
        Assert.Equal(2_500m, ex.RequestedAmount);
        Assert.Equal(1_000m, senderWallet.Balance); // Sender balance untouched
        Assert.Equal(500m, receiverWallet.Balance);   // Receiver balance untouched
    }

    [Fact]
    public async Task Transfer_Test2_RejectsSelfTransfer_WhenSenderAndReceiverAreIdentical()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = new BasicWallet(userId, "1234567890", initialBalance: 5_000m);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("1234567890", It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new TransferRequestDto("1234567890", Amount: 1_000m);

        // Act & Assert
        await Assert.ThrowsAsync<SelfTransferException>(() =>
            _transferService.TransferFundsAsync(userId, request));
    }

    [Fact]
    public async Task Transfer_Test3_BlocksTransfer_WhenSenderWalletIsFrozen()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var senderWallet = new BasicWallet(senderUserId, "1111111111", initialBalance: 5_000m);
        senderWallet.Freeze();
        var receiverWallet = new BasicWallet(receiverUserId, "2222222222", initialBalance: 500m);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(senderUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderWallet);
        _walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("2222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);

        var request = new TransferRequestDto("2222222222", Amount: 500m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WalletFrozenException>(() =>
            _transferService.TransferFundsAsync(senderUserId, request));

        Assert.Equal("1111111111", ex.WalletNumber);
    }

    [Fact]
    public async Task Transfer_BlocksTransfer_WhenReceiverWalletIsFrozen()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var senderWallet = new BasicWallet(senderUserId, "1111111111", initialBalance: 5_000m);
        var receiverWallet = new BasicWallet(receiverUserId, "2222222222", initialBalance: 500m);
        receiverWallet.Freeze();

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(senderUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderWallet);
        _walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("2222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);

        var request = new TransferRequestDto("2222222222", Amount: 500m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WalletFrozenException>(() =>
            _transferService.TransferFundsAsync(senderUserId, request));

        Assert.Equal("2222222222", ex.WalletNumber);
    }

    [Fact]
    public async Task Transfer_Test4_ExecutesSuccessfully_BetweenBasicWallets()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var senderWallet = new BasicWallet(senderUserId, "1111111111", initialBalance: 10_000m);
        var receiverWallet = new BasicWallet(receiverUserId, "2222222222", initialBalance: 2_000m);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(senderUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderWallet);
        _walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("2222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);
        _walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(senderWallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5_000m);

        var request = new TransferRequestDto("2222222222", Amount: 3_000m);

        // Act
        var result = await _transferService.TransferFundsAsync(senderUserId, request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("TX-", result.ReferenceId);
        Assert.Equal("1111111111", result.SenderWalletNumber);
        Assert.Equal("2222222222", result.ReceiverWalletNumber);
        Assert.Equal(3_000m, result.Amount);
        Assert.Equal(0m, result.Fee); // P2P fee for basic is 0
        Assert.Equal(3_000m, result.NetCreditedAmount);
        Assert.Equal(7_000m, result.SenderBalanceAfter);
        Assert.Equal(TransactionStatus.Completed, result.Status);

        Assert.Equal(7_000m, senderWallet.Balance);
        Assert.Equal(5_000m, receiverWallet.Balance);

        // Verify repository calls
        _walletRepoMock.Verify(r => r.Update(senderWallet), Times.Once);
        _walletRepoMock.Verify(r => r.Update(receiverWallet), Times.Once);
        _txRepoMock.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.Amount == 3_000m && t.Fee == 0m && t.Status == TransactionStatus.Completed), It.IsAny<CancellationToken>()), Times.Once);

        // Verify exactly 2 ledger entries were created (1 Debit, 1 Credit)
        _txRepoMock.Verify(r => r.AddLedgerEntryAsync(It.Is<LedgerEntry>(le =>
            le.EntryType == LedgerEntryType.Debit && le.Amount == 3_000m && le.BalanceAfter == 7_000m), It.IsAny<CancellationToken>()), Times.Once);

        _txRepoMock.Verify(r => r.AddLedgerEntryAsync(It.Is<LedgerEntry>(le =>
            le.EntryType == LedgerEntryType.Credit && le.Amount == 3_000m && le.BalanceAfter == 5_000m), It.IsAny<CancellationToken>()), Times.Once);

        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Transfer_Test5_MerchantFeeCalculation_DeductsHalfPercentFromReceiver()
    {
        // Arrange: Transfer to Merchant Wallet
        var senderUserId = Guid.NewGuid();
        var merchantUserId = Guid.NewGuid();
        var senderWallet = new BasicWallet(senderUserId, "1111111111", initialBalance: 20_000m);
        var merchantWallet = new MerchantWallet(merchantUserId, "9999999999", initialBalance: 10_000m);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(senderUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderWallet);
        _walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("9999999999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(merchantWallet);
        _walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(senderWallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var request = new TransferRequestDto("9999999999", Amount: 10_000m);

        // Act
        var result = await _transferService.TransferFundsAsync(senderUserId, request);

        // Assert
        // 0.5% of 10,000 = 50.00
        Assert.Equal(10_000m, result.Amount);
        Assert.Equal(50.00m, result.Fee);
        Assert.Equal(9_950.00m, result.NetCreditedAmount);
        Assert.Equal(10_000.00m, result.SenderBalanceAfter);

        Assert.Equal(10_000m, senderWallet.Balance);     // 20,000 - 10,000 = 10,000
        Assert.Equal(19_950m, merchantWallet.Balance);   // 10,000 + 9,950 = 19,950

        // Verify credit ledger row matches net credited
        _txRepoMock.Verify(r => r.AddLedgerEntryAsync(It.Is<LedgerEntry>(le =>
            le.WalletId == merchantWallet.Id &&
            le.EntryType == LedgerEntryType.Credit &&
            le.Amount == 9_950m &&
            le.BalanceAfter == 19_950m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Transfer_ThrowsDailyLimitExceededException_WhenBasicWalletLimitIsBreached()
    {
        // Arrange
        var senderUserId = Guid.NewGuid();
        var receiverUserId = Guid.NewGuid();
        var senderWallet = new BasicWallet(senderUserId, "1111111111", initialBalance: 60_000m);
        var receiverWallet = new BasicWallet(receiverUserId, "2222222222", initialBalance: 1_000m);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(senderUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(senderWallet);
        _walletRepoMock.Setup(r => r.GetByWalletNumberOrPhoneAsync("2222222222", It.IsAny<CancellationToken>()))
            .ReturnsAsync(receiverWallet);

        // Already transferred 40,000 today; attempting to transfer 15,000 (total = 55,000 > 50,000 limit)
        _walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(senderWallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(40_000m);

        var request = new TransferRequestDto("2222222222", Amount: 15_000m);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<DailyLimitExceededException>(() =>
            _transferService.TransferFundsAsync(senderUserId, request));

        Assert.Equal(50_000m, ex.DailyLimit);
        Assert.Equal(40_000m, ex.CurrentDayTotal);
        Assert.Equal(15_000m, ex.RequestedAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-500)]
    public async Task Transfer_ThrowsDomainValidationException_WhenAmountIsZeroOrNegative(decimal invalidAmount)
    {
        var senderUserId = Guid.NewGuid();
        var request = new TransferRequestDto("2222222222", Amount: invalidAmount);

        await Assert.ThrowsAsync<DomainValidationException>(() =>
            _transferService.TransferFundsAsync(senderUserId, request));
    }
}
