using FinPay.Application.DTOs.Transfer;

namespace FinPay.Application.Interfaces.Services;

public interface ITransferService
{
    Task<TransferResponseDto> TransferFundsAsync(Guid senderUserId, TransferRequestDto request, CancellationToken cancellationToken = default);
}
