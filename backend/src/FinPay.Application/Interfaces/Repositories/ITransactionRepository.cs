using FinPay.Application.DTOs.Transaction;
using FinPay.Domain.Entities;

namespace FinPay.Application.Interfaces.Repositories;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task AddLedgerEntryAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Transaction>> GetWalletHistoryAsync(Guid walletId, TransactionHistoryQueryDto query, CancellationToken cancellationToken = default);
    Task<int> CountWalletHistoryAsync(Guid walletId, TransactionHistoryQueryDto query, CancellationToken cancellationToken = default);
}
