using FinPay.Application.Common;
using FinPay.Application.DTOs.Transaction;
using FinPay.Application.DTOs.Wallet;

namespace FinPay.Application.Interfaces.Services;

public interface IWalletService
{
    Task<WalletResponseDto> GetMyWalletAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WalletResponseDto> DepositAsync(Guid userId, DepositRequestDto request, CancellationToken cancellationToken = default);
    Task<WalletResponseDto> FreezeWalletAsync(Guid walletId, bool isFrozen, string? reason = null, CancellationToken cancellationToken = default);
    Task<PagedResult<TransactionResponseDto>> GetHistoryAsync(Guid userId, TransactionHistoryQueryDto query, CancellationToken cancellationToken = default);
}
