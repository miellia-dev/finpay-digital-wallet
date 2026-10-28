namespace FinPay.Application.Exceptions;

public class DailyLimitExceededException : Exception
{
    public decimal DailyLimit { get; }
    public decimal CurrentDayTotal { get; }
    public decimal RequestedAmount { get; }

    public DailyLimitExceededException(decimal dailyLimit, decimal currentDayTotal, decimal requestedAmount)
        : base($"Daily transfer limit exceeded. Daily limit: PKR {dailyLimit:N2}, already transferred today: PKR {currentDayTotal:N2}, requested transfer: PKR {requestedAmount:N2}.")
    {
        DailyLimit = dailyLimit;
        CurrentDayTotal = currentDayTotal;
        RequestedAmount = requestedAmount;
    }
}
