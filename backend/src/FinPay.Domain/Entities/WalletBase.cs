using FinPay.Domain.Common;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;

namespace FinPay.Domain.Entities;

public abstract class WalletBase : BaseEntity
{
    public Guid UserId { get; protected set; }
    public string WalletNumber { get; protected set; } = string.Empty;
    public decimal Balance { get; protected set; }
    public WalletType WalletType { get; protected set; }
    public bool IsFrozen { get; protected set; }

    /// <summary>
    /// SQL Server timestamp / rowversion column used for optimistic concurrency control.
    /// </summary>
    public byte[] RowVersion { get; protected set; } = [];

    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; protected set; }
    public ICollection<Transaction> SentTransactions { get; protected set; } = new List<Transaction>();
    public ICollection<Transaction> ReceivedTransactions { get; protected set; } = new List<Transaction>();
    public ICollection<LedgerEntry> LedgerEntries { get; protected set; } = new List<LedgerEntry>();

    // Parameterless constructor for EF Core
    protected WalletBase() { }

    protected WalletBase(Guid userId, string walletNumber, WalletType walletType, decimal initialBalance = 0m)
    {
        if (userId == Guid.Empty)
            throw new DomainValidationException("UserId cannot be empty.");

        ValidateWalletNumber(walletNumber);

        if (initialBalance < 0)
            throw new DomainValidationException("Initial wallet balance cannot be negative.");

        UserId = userId;
        WalletNumber = walletNumber;
        WalletType = walletType;
        Balance = initialBalance;
        IsFrozen = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Polymorphic daily outgoing transfer limit in PKR.
    /// </summary>
    public abstract decimal DailyTransferLimit { get; }

    /// <summary>
    /// Polymorphic fee calculation for the wallet type.
    /// </summary>
    /// <param name="amount">The transaction amount.</param>
    /// <returns>Calculated transaction fee in PKR.</returns>
    public abstract decimal CalculateFee(decimal amount);

    /// <summary>
    /// Debits the specified amount from the wallet.
    /// Enforces non-negative balance invariant and active wallet status.
    /// </summary>
    public virtual void Debit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainValidationException($"Debit amount must be greater than zero. Received: {amount}.");

        if (IsFrozen)
            throw new WalletFrozenException(WalletNumber);

        if (Balance < amount)
            throw new InsufficientFundsException(Balance, amount);

        Balance -= amount;
    }

    /// <summary>
    /// Credits the specified amount to the wallet.
    /// Enforces active wallet status.
    /// </summary>
    public virtual void Credit(decimal amount)
    {
        if (amount <= 0)
            throw new DomainValidationException($"Credit amount must be greater than zero. Received: {amount}.");

        if (IsFrozen)
            throw new WalletFrozenException(WalletNumber);

        Balance += amount;
    }

    /// <summary>
    /// Freezes the wallet, preventing any debits or credits.
    /// </summary>
    public virtual void Freeze()
    {
        IsFrozen = true;
    }

    /// <summary>
    /// Unfreezes the wallet, restoring normal operation.
    /// </summary>
    public virtual void Unfreeze()
    {
        IsFrozen = false;
    }

    private static void ValidateWalletNumber(string walletNumber)
    {
        if (string.IsNullOrWhiteSpace(walletNumber))
            throw new DomainValidationException("Wallet number cannot be null or empty.");

        var trimmed = walletNumber.Trim();
        if (trimmed.Length != 10 || !trimmed.All(char.IsDigit))
            throw new DomainValidationException($"Wallet number must be exactly 10 numeric digits. Received: '{walletNumber}'.");
    }
}
