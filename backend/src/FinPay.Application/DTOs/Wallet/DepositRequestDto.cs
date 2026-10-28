namespace FinPay.Application.DTOs.Wallet;

/// <summary>
/// Simulated deposit request for development and testing.
/// </summary>
public record DepositRequestDto(
    decimal Amount,
    string? Notes = null);
