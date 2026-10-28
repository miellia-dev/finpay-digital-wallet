using FinPay.Domain.Enums;

namespace FinPay.Application.DTOs.Wallet;

public record WalletResponseDto(
    Guid WalletId,
    Guid UserId,
    string WalletNumber,
    decimal Balance,
    WalletType WalletType,
    bool IsFrozen,
    decimal DailyTransferLimit,
    decimal TransferredToday,
    decimal RemainingDailyLimit,
    DateTime CreatedAt);
