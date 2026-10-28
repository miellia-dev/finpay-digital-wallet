using FinPay.Application.Common;
using FinPay.Application.DTOs.Transaction;
using FinPay.Application.DTOs.Wallet;
using FinPay.Application.Exceptions;
using FinPay.Application.Interfaces;
using FinPay.Application.Interfaces.Repositories;
using FinPay.Application.Interfaces.Services;
using FinPay.Domain.Entities;
using FinPay.Domain.Enums;
using FinPay.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinPay.Application.Services;

public class WalletService : IWalletService
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<WalletService> logger)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<WalletResponseDto> GetMyWalletAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException("Wallet", $"UserId: {userId}");

        var todayUtc = DateTime.UtcNow.Date;
        var transferredToday = await _walletRepository.GetDailyTransferredAmountAsync(wallet.Id, todayUtc, cancellationToken);
        var remainingDailyLimit = Math.Max(0m, wallet.DailyTransferLimit - transferredToday);

        return new WalletResponseDto(
            WalletId: wallet.Id,
            UserId: wallet.UserId,
            WalletNumber: wallet.WalletNumber,
            Balance: wallet.Balance,
            WalletType: wallet.WalletType,
            IsFrozen: wallet.IsFrozen,
            DailyTransferLimit: wallet.DailyTransferLimit,
            TransferredToday: transferredToday,
            RemainingDailyLimit: remainingDailyLimit,
            CreatedAt: wallet.CreatedAt);
    }

    public async Task<WalletResponseDto> DepositAsync(
        Guid userId,
        DepositRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Amount <= 0)
            throw new DomainValidationException($"Deposit amount must be strictly greater than zero. Received: PKR {request.Amount:N2}.");

        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException("Wallet", $"UserId: {userId}");

        if (wallet.IsFrozen)
            throw new WalletFrozenException(wallet.WalletNumber);

        var referenceId = $"DEP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30].ToUpperInvariant();

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            wallet.Credit(request.Amount);

            var depositTx = Transaction.CreateDeposit(referenceId, wallet.Id, request.Amount);
            depositTx.MarkCompleted();

            var ledgerEntry = LedgerEntry.CreateCredit(
                depositTx.Id,
                wallet.Id,
                request.Amount,
                wallet.Balance);

            _walletRepository.Update(wallet);
            await _transactionRepository.AddAsync(depositTx, cancellationToken);
            await _transactionRepository.AddLedgerEntryAsync(ledgerEntry, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Deposit completed: Ref={Ref}, Wallet={Wallet}, Amount={Amount}",
                referenceId, wallet.WalletNumber, request.Amount);

            var todayUtc = DateTime.UtcNow.Date;
            var transferredToday = await _walletRepository.GetDailyTransferredAmountAsync(wallet.Id, todayUtc, cancellationToken);
            var remainingDailyLimit = Math.Max(0m, wallet.DailyTransferLimit - transferredToday);

            return new WalletResponseDto(
                WalletId: wallet.Id,
                UserId: wallet.UserId,
                WalletNumber: wallet.WalletNumber,
                Balance: wallet.Balance,
                WalletType: wallet.WalletType,
                IsFrozen: wallet.IsFrozen,
                DailyTransferLimit: wallet.DailyTransferLimit,
                TransferredToday: transferredToday,
                RemainingDailyLimit: remainingDailyLimit,
                CreatedAt: wallet.CreatedAt);
        }, cancellationToken);
    }

    public async Task<WalletResponseDto> FreezeWalletAsync(
        Guid walletId,
        bool isFrozen,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByIdAsync(walletId, cancellationToken)
            ?? throw new EntityNotFoundException("Wallet", walletId);

        if (isFrozen)
            wallet.Freeze();
        else
            wallet.Unfreeze();

        _walletRepository.Update(wallet);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Wallet {WalletNumber} freeze state changed to {IsFrozen}. Reason: {Reason}",
            wallet.WalletNumber, isFrozen, reason ?? "N/A");

        _ = _notificationService.NotifyWalletFrozenAsync(wallet.WalletNumber, reason, cancellationToken);

        var todayUtc = DateTime.UtcNow.Date;
        var transferredToday = await _walletRepository.GetDailyTransferredAmountAsync(wallet.Id, todayUtc, cancellationToken);
        var remainingDailyLimit = Math.Max(0m, wallet.DailyTransferLimit - transferredToday);

        return new WalletResponseDto(
            WalletId: wallet.Id,
            UserId: wallet.UserId,
            WalletNumber: wallet.WalletNumber,
            Balance: wallet.Balance,
            WalletType: wallet.WalletType,
            IsFrozen: wallet.IsFrozen,
            DailyTransferLimit: wallet.DailyTransferLimit,
            TransferredToday: transferredToday,
            RemainingDailyLimit: remainingDailyLimit,
            CreatedAt: wallet.CreatedAt);
    }

    public async Task<PagedResult<TransactionResponseDto>> GetHistoryAsync(
        Guid userId,
        TransactionHistoryQueryDto query,
        CancellationToken cancellationToken = default)
    {
        var wallet = await _walletRepository.GetByUserIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException("Wallet", $"UserId: {userId}");

        var totalCount = await _transactionRepository.CountWalletHistoryAsync(wallet.Id, query, cancellationToken);
        var transactions = await _transactionRepository.GetWalletHistoryAsync(wallet.Id, query, cancellationToken);

        var dtos = transactions.Select(tx =>
        {
            var isSender = tx.SenderWalletId == wallet.Id;
            var direction = isSender ? "Debit" : "Credit";
            var matchingLedger = tx.LedgerEntries.FirstOrDefault(le => le.WalletId == wallet.Id);

            return new TransactionResponseDto(
                Id: tx.Id,
                ReferenceId: tx.ReferenceId,
                SenderWalletNumber: tx.SenderWallet?.WalletNumber,
                ReceiverWalletNumber: tx.ReceiverWallet?.WalletNumber ?? string.Empty,
                Amount: tx.Amount,
                Fee: tx.Fee,
                Type: tx.Type,
                Status: tx.Status,
                CreatedAt: tx.CreatedAt,
                Direction: direction,
                BalanceAfter: matchingLedger?.BalanceAfter);
        }).ToList();

        return new PagedResult<TransactionResponseDto>(dtos, totalCount, query.PageNumber, query.PageSize);
    }
}
