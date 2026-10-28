using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;
using Xunit;

namespace FinPay.Tests.Unit.Domain;

public class DomainEntityTests
{
    [Fact]
    public void Transaction_RejectsSelfTransfer_WhenSenderAndReceiverAreIdentical()
    {
        var walletId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() =>
            new Transaction(
                referenceId: "TX-123456",
                senderWalletId: walletId,
                receiverWalletId: walletId,
                amount: 100m,
                fee: 0m));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public void Transaction_RejectsInvalidAmount(decimal invalidAmount)
    {
        var senderId = Guid.NewGuid();
        var receiverId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() =>
            new Transaction(
                referenceId: "TX-123456",
                senderWalletId: senderId,
                receiverWalletId: receiverId,
                amount: invalidAmount,
                fee: 0m));
    }

    [Fact]
    public void Transaction_AllowsNullSender_ForDeposits()
    {
        var receiverId = Guid.NewGuid();

        var deposit = Transaction.CreateDeposit("DEP-99999", receiverId, 5000m);

        Assert.Null(deposit.SenderWalletId);
        Assert.Equal(receiverId, deposit.ReceiverWalletId);
        Assert.Equal(5000m, deposit.Amount);
        Assert.Equal(0m, deposit.Fee);
        Assert.Equal(TransactionType.Deposit, deposit.Type);
        Assert.Equal(TransactionStatus.Pending, deposit.Status);

        deposit.MarkCompleted();
        Assert.Equal(TransactionStatus.Completed, deposit.Status);
    }

    [Fact]
    public void LedgerEntry_ThrowsDomainValidationException_WhenBalanceAfterIsNegative()
    {
        var txId = Guid.NewGuid();
        var walletId = Guid.NewGuid();

        Assert.Throws<DomainValidationException>(() =>
            LedgerEntry.CreateDebit(txId, walletId, 100m, balanceAfter: -10m));
    }

    [Fact]
    public void User_RejectsInvalidEmail()
    {
        Assert.Throws<DomainValidationException>(() =>
            new User("John Doe", "invalid-email-no-at", "+923001234567", "hashed_pwd"));
    }
}
