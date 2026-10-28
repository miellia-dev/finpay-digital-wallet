namespace FinPay.Application.Exceptions;

public class SelfTransferException : Exception
{
    public SelfTransferException(string message = "Self-transfer is forbidden. Sender and receiver cannot be the same wallet.")
        : base(message)
    {
    }
}
