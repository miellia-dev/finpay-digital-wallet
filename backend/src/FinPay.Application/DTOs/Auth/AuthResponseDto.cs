using FinPay.Domain.Enums;

namespace FinPay.Application.DTOs.Auth;

public record AuthResponseDto(
    Guid UserId,
    string FullName,
    string Email,
    UserRole Role,
    string WalletNumber,
    string Token,
    DateTime ExpiresAt);
