using FinPay.Domain.Entities;

namespace FinPay.Application.Interfaces.Repositories;

public interface IWalletRepository
{
    Task<WalletBase?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WalletBase?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WalletBase?> GetByWalletNumberAsync(string walletNumber, CancellationToken cancellationToken = default);
    Task<WalletBase?> GetByWalletNumberOrPhoneAsync(string identifier, CancellationToken cancellationToken = default);
    Task<bool> ExistsByWalletNumberAsync(string walletNumber, CancellationToken cancellationToken = default);
    Task<decimal> GetDailyTransferredAmountAsync(Guid walletId, DateTime fromUtc, CancellationToken cancellationToken = default);
    Task AddAsync(WalletBase wallet, CancellationToken cancellationToken = default);
    void Update(WalletBase wallet);
}
