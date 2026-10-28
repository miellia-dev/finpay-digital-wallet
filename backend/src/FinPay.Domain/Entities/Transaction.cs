using FinPay.Domain.Common;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;

namespace FinPay.Domain.Entities;

public class Transaction : BaseEntity
{
    public string ReferenceId { get; private set; } = string.Empty;
    public Guid? SenderWalletId { get; private set; }
    public Guid ReceiverWalletId { get; private set; }
    public decimal Amount { get; private set; }
    public decimal Fee { get; private set; }
    public TransactionType Type { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public WalletBase? SenderWallet { get; private set; }
    public WalletBase? ReceiverWallet { get; private set; }
    public ICollection<LedgerEntry> LedgerEntries { get; private set; } = new List<LedgerEntry>();

    // Parameterless constructor for EF Core
    protected Transaction() { }

    public Transaction(
        string referenceId,
        Guid? senderWalletId,
        Guid receiverWalletId,
        decimal amount,
        decimal fee,
        TransactionType type = TransactionType.P2PTransfer)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new DomainValidationException("Transaction reference ID cannot be null or empty.");

        if (receiverWalletId == Guid.Empty)
            throw new DomainValidationException("Receiver wallet ID must be specified.");

        if (senderWalletId.HasValue && senderWalletId.Value == receiverWalletId)
            throw new DomainValidationException("Sender wallet and receiver wallet cannot be identical (self-transfer forbidden).");

        if (amount <= 0)
            throw new DomainValidationException($"Transaction amount must be strictly greater than zero. Received: {amount}.");

        if (fee < 0)
            throw new DomainValidationException($"Transaction fee cannot be negative. Received: {fee}.");

        ReferenceId = referenceId.Trim();
        SenderWalletId = senderWalletId;
        ReceiverWalletId = receiverWalletId;
        Amount = amount;
        Fee = fee;
        Type = type;
        Status = TransactionStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory for creating a deposit transaction (system-originated, null sender).
    /// </summary>
    public static Transaction CreateDeposit(string referenceId, Guid receiverWalletId, decimal amount)
    {
        return new Transaction(
            referenceId,
            senderWalletId: null,
            receiverWalletId: receiverWalletId,
            amount: amount,
            fee: 0m,
            type: TransactionType.Deposit);
    }

    public void MarkCompleted()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainValidationException("Transaction is already marked as completed.");

        Status = TransactionStatus.Completed;
    }

    public void MarkFailed()
    {
        if (Status == TransactionStatus.Completed)
            throw new DomainValidationException("Cannot mark a completed transaction as failed.");

        Status = TransactionStatus.Failed;
    }
}
