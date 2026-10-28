using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;
using Xunit;

namespace FinPay.Tests.Unit.Domain;

public class WalletDomainTests
{
    private static readonly Guid TestUserId = Guid.NewGuid();
    private const string ValidWalletNumber = "1234567890";

    [Fact]
    public void BasicWallet_InitializesWithCorrectDefaults()
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber, 1000m);

        Assert.Equal(TestUserId, wallet.UserId);
        Assert.Equal(ValidWalletNumber, wallet.WalletNumber);
        Assert.Equal(1000m, wallet.Balance);
        Assert.Equal(WalletType.Basic, wallet.WalletType);
        Assert.False(wallet.IsFrozen);
        Assert.Equal(50_000m, wallet.DailyTransferLimit);
    }

    [Fact]
    public void MerchantWallet_InitializesWithCorrectDefaults()
    {
        var wallet = new MerchantWallet(TestUserId, ValidWalletNumber, 5000m);

        Assert.Equal(TestUserId, wallet.UserId);
        Assert.Equal(ValidWalletNumber, wallet.WalletNumber);
        Assert.Equal(5000m, wallet.Balance);
        Assert.Equal(WalletType.Merchant, wallet.WalletType);
        Assert.False(wallet.IsFrozen);
        Assert.Equal(1_000_000m, wallet.DailyTransferLimit);
    }

    [Fact]
    public void CalculateFee_BasicWallet_ReturnsZeroForP2P()
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber);

        var fee = wallet.CalculateFee(5000m);

        Assert.Equal(0m, fee);
    }

    [Theory]
    [InlineData(1000, 5.00)]
    [InlineData(5000, 25.00)]
    [InlineData(100.50, 0.50)]
    [InlineData(0, 0.00)]
    public void CalculateFee_MerchantWallet_DeductsHalfPercent(decimal amount, decimal expectedFee)
    {
        var wallet = new MerchantWallet(TestUserId, ValidWalletNumber);

        var fee = wallet.CalculateFee(amount);

        Assert.Equal(expectedFee, fee);
    }

    [Fact]
    public void Credit_IncreasesBalance_WhenAmountIsValid()
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber, 500m);

        wallet.Credit(250m);

        Assert.Equal(750m, wallet.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void Credit_ThrowsDomainValidationException_WhenAmountIsZeroOrNegative(decimal invalidAmount)
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber, 500m);

        Assert.Throws<DomainValidationException>(() => wallet.Credit(invalidAmount));
    }

    [Fact]
    public void Debit_DecreasesBalance_WhenFundsAreSufficient()
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber, 500m);

        wallet.Debit(200m);

        Assert.Equal(300m, wallet.Balance);
    }

    [Fact]
    public void Debit_ThrowsInsufficientFundsException_WhenFundsAreInsufficient()
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber, 100m);

        var ex = Assert.Throws<InsufficientFundsException>(() => wallet.Debit(250m));
        Assert.Equal(100m, ex.AvailableBalance);
        Assert.Equal(250m, ex.RequestedAmount);
    }

    [Fact]
    public void Freeze_BlocksBothDebitAndCredit()
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber, 500m);

        wallet.Freeze();

        Assert.True(wallet.IsFrozen);
        Assert.Throws<WalletFrozenException>(() => wallet.Debit(100m));
        Assert.Throws<WalletFrozenException>(() => wallet.Credit(100m));
    }

    [Fact]
    public void Unfreeze_RestoresNormalWalletOperations()
    {
        var wallet = new BasicWallet(TestUserId, ValidWalletNumber, 500m);
        wallet.Freeze();

        wallet.Unfreeze();

        Assert.False(wallet.IsFrozen);
        wallet.Debit(100m);
        Assert.Equal(400m, wallet.Balance);
    }

    [Theory]
    [InlineData("12345")]       // too short
    [InlineData("12345678901")]  // too long
    [InlineData("123456789a")]  // contains non-digit
    [InlineData("")]            // empty
    public void Wallet_ThrowsException_WhenWalletNumberIsNot10Digits(string invalidWalletNumber)
    {
        Assert.Throws<DomainValidationException>(() =>
            new BasicWallet(TestUserId, invalidWalletNumber));
    }
}
