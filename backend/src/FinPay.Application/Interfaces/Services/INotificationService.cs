namespace FinPay.Application.Interfaces.Services;

public interface INotificationService
{
    Task NotifyTransferCompletedAsync(string senderWalletNumber, string receiverWalletNumber, decimal amount, decimal fee, CancellationToken cancellationToken = default);
    Task NotifyWalletFrozenAsync(string walletNumber, string? reason, CancellationToken cancellationToken = default);
}
