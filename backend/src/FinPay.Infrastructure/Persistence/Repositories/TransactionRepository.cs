using FinPay.Application.DTOs.Transaction;
using FinPay.Application.Interfaces.Repositories;
using FinPay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinPay.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .Include(t => t.SenderWallet)
            .Include(t => t.ReceiverWallet)
            .Include(t => t.LedgerEntries)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
    {
        var trimmed = referenceId.Trim();
        return await _context.Transactions
            .Include(t => t.SenderWallet)
            .Include(t => t.ReceiverWallet)
            .Include(t => t.LedgerEntries)
            .FirstOrDefaultAsync(t => t.ReferenceId == trimmed, cancellationToken);
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
    }

    public async Task AddLedgerEntryAsync(LedgerEntry ledgerEntry, CancellationToken cancellationToken = default)
    {
        await _context.LedgerEntries.AddAsync(ledgerEntry, cancellationToken);
    }

    public async Task<IReadOnlyList<Transaction>> GetWalletHistoryAsync(
        Guid walletId,
        TransactionHistoryQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.Transactions
            .AsNoTracking()
            .Include(t => t.SenderWallet)
            .Include(t => t.ReceiverWallet)
            .Include(t => t.LedgerEntries)
            .Where(t => t.SenderWalletId == walletId || t.ReceiverWalletId == walletId);

        if (query.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.CreatedAt <= query.EndDate.Value);
        }

        if (query.Type.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.Type == query.Type.Value);
        }

        var skip = (query.PageNumber - 1) * query.PageSize;

        return await baseQuery
            .OrderByDescending(t => t.CreatedAt)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountWalletHistoryAsync(
        Guid walletId,
        TransactionHistoryQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _context.Transactions
            .AsNoTracking()
            .Where(t => t.SenderWalletId == walletId || t.ReceiverWalletId == walletId);

        if (query.StartDate.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.CreatedAt <= query.EndDate.Value);
        }

        if (query.Type.HasValue)
        {
            baseQuery = baseQuery.Where(t => t.Type == query.Type.Value);
        }

        return await baseQuery.CountAsync(cancellationToken);
    }
}
