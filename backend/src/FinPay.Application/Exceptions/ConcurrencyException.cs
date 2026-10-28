namespace FinPay.Application.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message = "The resource was modified concurrently by another transaction. Please retry.")
        : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
