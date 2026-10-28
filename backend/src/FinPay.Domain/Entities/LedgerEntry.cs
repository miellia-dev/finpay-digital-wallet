using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;

namespace FinPay.Domain.Entities;

/// <summary>
/// Immutable double-entry ledger record representing a financial movement.
/// Normal application operations must never edit or delete rows from this table.
/// </summary>
public class LedgerEntry
{
    public long Id { get; private set; }
    public Guid TransactionId { get; private set; }
    public Guid WalletId { get; private set; }
    public LedgerEntryType EntryType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    // Navigation properties
    public Transaction? Transaction { get; private set; }
    public WalletBase? Wallet { get; private set; }

    // Parameterless constructor for EF Core
    protected LedgerEntry() { }

    public LedgerEntry(
        Guid transactionId,
        Guid walletId,
        LedgerEntryType entryType,
        decimal amount,
        decimal balanceAfter)
    {
        if (transactionId == Guid.Empty)
            throw new DomainValidationException("TransactionId cannot be empty for ledger entry.");

        if (walletId == Guid.Empty)
            throw new DomainValidationException("WalletId cannot be empty for ledger entry.");

        if (amount <= 0)
            throw new DomainValidationException($"Ledger entry amount must be greater than zero. Received: {amount}.");

        if (balanceAfter < 0)
            throw new DomainValidationException($"Balance after transaction cannot be negative. Received: {balanceAfter}.");

        TransactionId = transactionId;
        WalletId = walletId;
        EntryType = entryType;
        Amount = amount;
        BalanceAfter = balanceAfter;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory for creating a Debit ledger entry.
    /// </summary>
    public static LedgerEntry CreateDebit(Guid transactionId, Guid walletId, decimal amount, decimal balanceAfter)
    {
        return new LedgerEntry(transactionId, walletId, LedgerEntryType.Debit, amount, balanceAfter);
    }

    /// <summary>
    /// Factory for creating a Credit ledger entry.
    /// </summary>
    public static LedgerEntry CreateCredit(Guid transactionId, Guid walletId, decimal amount, decimal balanceAfter)
    {
        return new LedgerEntry(transactionId, walletId, LedgerEntryType.Credit, amount, balanceAfter);
    }
}
