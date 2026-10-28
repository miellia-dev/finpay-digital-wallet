namespace FinPay.Domain.Exceptions;

public class WalletFrozenException : Exception
{
    public string WalletNumber { get; }

    public WalletFrozenException(string walletNumber)
        : base($"Wallet '{walletNumber}' is frozen. Financial transactions are blocked.")
    {
        WalletNumber = walletNumber;
    }
}
