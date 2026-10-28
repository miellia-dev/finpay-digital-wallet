using FinPay.Domain.Enums;

namespace FinPay.Application.DTOs.Auth;

public record RegisterRequestDto(
    string FullName,
    string Email,
    string PhoneNumber,
    string Password,
    UserRole Role = UserRole.Customer);
