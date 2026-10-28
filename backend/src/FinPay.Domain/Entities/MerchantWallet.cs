using FinPay.Domain.Enums;

namespace FinPay.Domain.Entities;

/// <summary>
/// Level 2 Merchant / Business Wallet:
/// Daily Transfer Limit: PKR 1,000,000.
/// Incoming Payment Service Tax: 0.5% deducted from each incoming payment.
/// </summary>
public class MerchantWallet : WalletBase
{
    public const decimal DefaultDailyLimit = 1_000_000m;
    public const decimal IncomingServiceTaxRate = 0.005m; // 0.5%

    public override decimal DailyTransferLimit => DefaultDailyLimit;

    // Parameterless constructor for EF Core
    protected MerchantWallet() { }

    public MerchantWallet(Guid userId, string walletNumber, decimal initialBalance = 0m)
        : base(userId, walletNumber, WalletType.Merchant, initialBalance)
    {
    }

    /// <summary>
    /// Calculates the 0.5% service charge applied to incoming payments.
    /// Rounded to two decimal places (half away from zero) to preserve financial precision.
    /// </summary>
    public override decimal CalculateFee(decimal amount)
    {
        if (amount <= 0) return 0m;
        return Math.Round(amount * IncomingServiceTaxRate, 2, MidpointRounding.AwayFromZero);
    }
}
