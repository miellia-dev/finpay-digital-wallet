namespace FinPay.Application.DTOs.Wallet;

public record FreezeWalletRequestDto(
    bool IsFrozen,
    string? Reason = null);
