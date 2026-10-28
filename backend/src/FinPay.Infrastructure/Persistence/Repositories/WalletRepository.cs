using FinPay.Application.Interfaces.Repositories;
using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinPay.Infrastructure.Persistence.Repositories;

public class WalletRepository : IWalletRepository
{
    private readonly AppDbContext _context;

    public WalletRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<WalletBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public async Task<WalletBase?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
    }

    public async Task<WalletBase?> GetByWalletNumberAsync(string walletNumber, CancellationToken cancellationToken = default)
    {
        var trimmed = walletNumber.Trim();
        return await _context.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.WalletNumber == trimmed, cancellationToken);
    }

    public async Task<WalletBase?> GetByWalletNumberOrPhoneAsync(string identifier, CancellationToken cancellationToken = default)
    {
        var trimmed = identifier.Trim();

        // Check if identifier matches 10-digit wallet number first
        var wallet = await _context.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.WalletNumber == trimmed, cancellationToken);

        if (wallet != null)
            return wallet;

        // Otherwise check by linked user's phone number
        return await _context.Wallets
            .Include(w => w.User)
            .FirstOrDefaultAsync(w => w.User != null && w.User.PhoneNumber == trimmed, cancellationToken);
    }

    public async Task<bool> ExistsByWalletNumberAsync(string walletNumber, CancellationToken cancellationToken = default)
    {
        var trimmed = walletNumber.Trim();
        return await _context.Wallets
            .AsNoTracking()
            .AnyAsync(w => w.WalletNumber == trimmed, cancellationToken);
    }

    public async Task<decimal> GetDailyTransferredAmountAsync(Guid walletId, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        // Enforce LINQ Sum() for daily outgoing transfers
        var sum = await _context.Transactions
            .AsNoTracking()
            .Where(t => t.SenderWalletId == walletId &&
                        t.Status == TransactionStatus.Completed &&
                        t.CreatedAt >= fromUtc)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken);

        return sum ?? 0m;
    }

    public async Task AddAsync(WalletBase wallet, CancellationToken cancellationToken = default)
    {
        await _context.Wallets.AddAsync(wallet, cancellationToken);
    }

    public void Update(WalletBase wallet)
    {
        _context.Wallets.Update(wallet);
    }
}
