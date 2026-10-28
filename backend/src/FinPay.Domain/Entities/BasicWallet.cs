using FinPay.Domain.Enums;

namespace FinPay.Domain.Entities;

/// <summary>
/// Level 1 Basic Wallet:
/// Daily Transfer Limit: PKR 50,000.
/// P2P Transfer Fee: PKR 0.
/// ATM/Bank Withdrawal Fee: 1.5%.
/// </summary>
public class BasicWallet : WalletBase
{
    public const decimal DefaultDailyLimit = 50_000m;
    public const decimal P2PFeeRate = 0m;
    public const decimal WithdrawalFeeRate = 0.015m;

    public override decimal DailyTransferLimit => DefaultDailyLimit;

    // Parameterless constructor for EF Core
    protected BasicWallet() { }

    public BasicWallet(Guid userId, string walletNumber, decimal initialBalance = 0m)
        : base(userId, walletNumber, WalletType.Basic, initialBalance)
    {
    }

    /// <summary>
    /// Basic wallet transfers incur PKR 0 fee for P2P transactions.
    /// </summary>
    public override decimal CalculateFee(decimal amount)
    {
        return 0m;
    }

    /// <summary>
    /// Calculates ATM/bank withdrawal fee (1.5%).
    /// </summary>
    public decimal CalculateWithdrawalFee(decimal amount)
    {
        if (amount <= 0) return 0m;
        return Math.Round(amount * WithdrawalFeeRate, 2, MidpointRounding.AwayFromZero);
    }
}
