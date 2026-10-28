using FinPay.Domain.Enums;

namespace FinPay.Application.DTOs.Transaction;

public record TransactionResponseDto(
    Guid Id,
    string ReferenceId,
    string? SenderWalletNumber,
    string ReceiverWalletNumber,
    decimal Amount,
    decimal Fee,
    TransactionType Type,
    TransactionStatus Status,
    DateTime CreatedAt,
    string Direction, // "Debit", "Credit", or "Self"
    decimal? BalanceAfter);
