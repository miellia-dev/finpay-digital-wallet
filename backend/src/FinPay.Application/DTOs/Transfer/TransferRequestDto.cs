namespace FinPay.Application.DTOs.Transfer;

/// <summary>
/// Transfer request DTO. Supports recipient by 10-digit wallet number or phone number.
/// </summary>
public record TransferRequestDto(
    string ReceiverWalletNumber,
    decimal Amount,
    string? Notes = null);
