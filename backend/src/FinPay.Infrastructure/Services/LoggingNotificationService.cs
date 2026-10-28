using FinPay.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace FinPay.Infrastructure.Services;

public class LoggingNotificationService : INotificationService
{
    private readonly ILogger<LoggingNotificationService> _logger;

    public LoggingNotificationService(ILogger<LoggingNotificationService> logger)
    {
        _logger = logger;
    }

    public Task NotifyTransferCompletedAsync(
        string senderWalletNumber,
        string receiverWalletNumber,
        decimal amount,
        decimal fee,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NOTIFICATION] Transfer Completed: From {Sender} To {Receiver}, Amount: PKR {Amount:N2}, Fee: PKR {Fee:N2}",
            senderWalletNumber, receiverWalletNumber, amount, fee);

        return Task.CompletedTask;
    }

    public Task NotifyWalletFrozenAsync(
        string walletNumber,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[NOTIFICATION] Wallet State Changed: Wallet {WalletNumber} frozen/unfrozen. Reason: {Reason}",
            walletNumber, reason ?? "Unspecified");

        return Task.CompletedTask;
    }
}
