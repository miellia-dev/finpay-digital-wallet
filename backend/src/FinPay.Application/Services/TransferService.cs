using FinPay.Application.DTOs.Transfer;
using FinPay.Application.Exceptions;
using FinPay.Application.Interfaces;
using FinPay.Application.Interfaces.Repositories;
using FinPay.Application.Interfaces.Services;
using FinPay.Domain.Entities;
using FinPay.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace FinPay.Application.Services;

public class TransferService : ITransferService
{
    private readonly IWalletRepository _walletRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationService _notificationService;
    private readonly ILogger<TransferService> _logger;

    public TransferService(
        IWalletRepository walletRepository,
        ITransactionRepository transactionRepository,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        ILogger<TransferService> logger)
    {
        _walletRepository = walletRepository;
        _transactionRepository = transactionRepository;
        _unitOfWork = unitOfWork;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<TransferResponseDto> TransferFundsAsync(
        Guid senderUserId,
        TransferRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ReceiverWalletNumber))
            throw new ValidationException("Receiver wallet number or identifier is required.");

        if (request.Amount <= 0)
            throw new DomainValidationException($"Transfer amount must be strictly greater than zero. Received: PKR {request.Amount:N2}.");

        // 1. Resolve sender wallet strictly from authenticated user context
        var senderWallet = await _walletRepository.GetByUserIdAsync(senderUserId, cancellationToken)
            ?? throw new EntityNotFoundException("Wallet", $"UserId: {senderUserId}");

        // 2. Resolve receiver wallet by wallet number or phone number
        var receiverWallet = await _walletRepository.GetByWalletNumberOrPhoneAsync(request.ReceiverWalletNumber.Trim(), cancellationToken)
            ?? throw new EntityNotFoundException("Receiver wallet", request.ReceiverWalletNumber);

        // 3. Self-transfer prevention
        if (senderWallet.Id == receiverWallet.Id)
        {
            _logger.LogWarning("Self-transfer rejected for wallet {WalletNumber}", senderWallet.WalletNumber);
            throw new SelfTransferException($"Self-transfers are not allowed. Sender and receiver wallet numbers are identical ({senderWallet.WalletNumber}).");
        }

        // 4. Frozen wallet checks (both sender and receiver must be active)
        if (senderWallet.IsFrozen)
        {
            _logger.LogWarning("Transfer rejected: Sender wallet {WalletNumber} is frozen", senderWallet.WalletNumber);
            throw new WalletFrozenException(senderWallet.WalletNumber);
        }

        if (receiverWallet.IsFrozen)
        {
            _logger.LogWarning("Transfer rejected: Receiver wallet {WalletNumber} is frozen", receiverWallet.WalletNumber);
            throw new WalletFrozenException(receiverWallet.WalletNumber);
        }

        // 5. Daily limit enforcement inside transaction boundary
        var todayUtc = DateTime.UtcNow.Date;
        var dailyTransferred = await _walletRepository.GetDailyTransferredAmountAsync(senderWallet.Id, todayUtc, cancellationToken);
        if (dailyTransferred + request.Amount > senderWallet.DailyTransferLimit)
        {
            _logger.LogWarning(
                "Daily transfer limit breach on wallet {WalletNumber}. Limit: {Limit}, Used: {Used}, Requested: {Amount}",
                senderWallet.WalletNumber, senderWallet.DailyTransferLimit, dailyTransferred, request.Amount);
            throw new DailyLimitExceededException(senderWallet.DailyTransferLimit, dailyTransferred, request.Amount);
        }

        // 6. Fee calculation (polymorphic based on receiver's wallet tier)
        var fee = receiverWallet.CalculateFee(request.Amount);
        var netCreditedAmount = request.Amount - fee;

        if (netCreditedAmount <= 0)
            throw new DomainValidationException("Net credited amount after fee deduction must be greater than zero.");

        // 7. Generate unique transaction reference
        var referenceId = $"TX-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30].ToUpperInvariant();

        // 8. Execute atomically inside UnitOfWork transaction
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Debit sender wallet (encapsulated check for sufficient funds)
            senderWallet.Debit(request.Amount);

            // Credit receiver wallet (net of calculated service charges)
            receiverWallet.Credit(netCreditedAmount);

            // Create Transaction record
            var transaction = new Transaction(
                referenceId: referenceId,
                senderWalletId: senderWallet.Id,
                receiverWalletId: receiverWallet.Id,
                amount: request.Amount,
                fee: fee,
                type: Domain.Enums.TransactionType.P2PTransfer);

            transaction.MarkCompleted();

            // Create double-entry ledger records
            var senderDebitEntry = LedgerEntry.CreateDebit(
                transaction.Id,
                senderWallet.Id,
                request.Amount,
                senderWallet.Balance);

            var receiverCreditEntry = LedgerEntry.CreateCredit(
                transaction.Id,
                receiverWallet.Id,
                netCreditedAmount,
                receiverWallet.Balance);

            // Persist changes
            _walletRepository.Update(senderWallet);
            _walletRepository.Update(receiverWallet);
            await _transactionRepository.AddAsync(transaction, cancellationToken);
            await _transactionRepository.AddLedgerEntryAsync(senderDebitEntry, cancellationToken);
            await _transactionRepository.AddLedgerEntryAsync(receiverCreditEntry, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "P2P Transfer completed: Ref={Ref}, Sender={Sender}, Receiver={Receiver}, Amount={Amount}, Fee={Fee}",
                referenceId, senderWallet.WalletNumber, receiverWallet.WalletNumber, request.Amount, fee);

            // Trigger non-blocking notification
            _ = _notificationService.NotifyTransferCompletedAsync(
                senderWallet.WalletNumber,
                receiverWallet.WalletNumber,
                request.Amount,
                fee,
                cancellationToken);

            return new TransferResponseDto(
                ReferenceId: transaction.ReferenceId,
                SenderWalletNumber: senderWallet.WalletNumber,
                ReceiverWalletNumber: receiverWallet.WalletNumber,
                Amount: transaction.Amount,
                Fee: transaction.Fee,
                NetCreditedAmount: netCreditedAmount,
                SenderBalanceAfter: senderWallet.Balance,
                Status: transaction.Status,
                CreatedAt: transaction.CreatedAt);
        }, cancellationToken);
    }
}
