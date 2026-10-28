using FinPay.Domain.Enums;

namespace FinPay.Application.DTOs.Transfer;

public record TransferResponseDto(
    string ReferenceId,
    string SenderWalletNumber,
    string ReceiverWalletNumber,
    decimal Amount,
    decimal Fee,
    decimal NetCreditedAmount,
    decimal SenderBalanceAfter,
    TransactionStatus Status,
    DateTime CreatedAt);
