using FinPay.Domain.Enums;

namespace FinPay.Application.DTOs.Transaction;

public record TransactionHistoryQueryDto
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }

    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public TransactionType? Type { get; init; }
}
