namespace FinPay.Domain.Exceptions;

public class InsufficientFundsException : Exception
{
    public decimal AvailableBalance { get; }
    public decimal RequestedAmount { get; }

    public InsufficientFundsException(decimal availableBalance, decimal requestedAmount)
        : base($"Insufficient funds for operation. Available balance: PKR {availableBalance:N2}, Requested amount: PKR {requestedAmount:N2}.")
    {
        AvailableBalance = availableBalance;
        RequestedAmount = requestedAmount;
    }
}
