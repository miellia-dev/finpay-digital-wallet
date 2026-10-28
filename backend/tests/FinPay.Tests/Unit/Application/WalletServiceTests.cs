using FinPay.Application.DTOs.Wallet;
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

public class WalletServiceTests
{
    private readonly Mock<IWalletRepository> _walletRepoMock;
    private readonly Mock<ITransactionRepository> _txRepoMock;
    private readonly Mock<IUnitOfWork> _uowMock;
    private readonly Mock<INotificationService> _notificationMock;
    private readonly Mock<ILogger<WalletService>> _loggerMock;
    private readonly WalletService _walletService;

    public WalletServiceTests()
    {
        _walletRepoMock = new Mock<IWalletRepository>();
        _txRepoMock = new Mock<ITransactionRepository>();
        _uowMock = new Mock<IUnitOfWork>();
        _notificationMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<WalletService>>();

        _uowMock.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task<WalletResponseDto>>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task<WalletResponseDto>>, CancellationToken>((func, _) => func());

        _walletService = new WalletService(
            _walletRepoMock.Object,
            _txRepoMock.Object,
            _uowMock.Object,
            _notificationMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DepositAsync_CreditsWallet_AndRecordsTransactionAndLedger()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = new BasicWallet(userId, "1234567890", initialBalance: 1_000m);

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);
        _walletRepoMock.Setup(r => r.GetDailyTransferredAmountAsync(wallet.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        var request = new DepositRequestDto(Amount: 5_000m);

        // Act
        var result = await _walletService.DepositAsync(userId, request);

        // Assert
        Assert.Equal(6_000m, result.Balance);
        Assert.Equal(6_000m, wallet.Balance);

        _txRepoMock.Verify(r => r.AddAsync(It.Is<Transaction>(t =>
            t.SenderWalletId == null &&
            t.ReceiverWalletId == wallet.Id &&
            t.Amount == 5_000m &&
            t.Type == TransactionType.Deposit), It.IsAny<CancellationToken>()), Times.Once);

        _txRepoMock.Verify(r => r.AddLedgerEntryAsync(It.Is<LedgerEntry>(le =>
            le.WalletId == wallet.Id &&
            le.EntryType == LedgerEntryType.Credit &&
            le.Amount == 5_000m &&
            le.BalanceAfter == 6_000m), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DepositAsync_ThrowsWalletFrozenException_WhenWalletIsFrozen()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var wallet = new BasicWallet(userId, "1234567890", initialBalance: 1_000m);
        wallet.Freeze();

        _walletRepoMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        var request = new DepositRequestDto(Amount: 500m);

        // Act & Assert
        await Assert.ThrowsAsync<WalletFrozenException>(() =>
            _walletService.DepositAsync(userId, request));
    }

    [Fact]
    public async Task FreezeWalletAsync_UpdatesStateAndNotifies()
    {
        // Arrange
        var walletId = Guid.NewGuid();
        var wallet = new BasicWallet(Guid.NewGuid(), "1234567890", initialBalance: 2_000m);

        _walletRepoMock.Setup(r => r.GetByIdAsync(walletId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(wallet);

        // Act
        var result = await _walletService.FreezeWalletAsync(walletId, isFrozen: true, reason: "Suspicious velocity");

        // Assert
        Assert.True(result.IsFrozen);
        Assert.True(wallet.IsFrozen);
        _walletRepoMock.Verify(r => r.Update(wallet), Times.Once);
        _uowMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
