import React, { useState, useEffect } from 'react';
import { walletService } from '../services/walletService';
import { transferService } from '../services/transferService';
import type { WalletDetails } from '../types/wallet';
import type { TransferResponse } from '../types/transfer';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { ErrorMessage } from '../components/Common/ErrorMessage';
import { ConfirmModal } from '../components/Common/ConfirmModal';
import {
  Send,
  UserCheck,
  CheckCircle2,
  ArrowRight,
  ShieldAlert,
  RotateCcw
} from 'lucide-react';

export const SendMoneyPage: React.FC = () => {
  const [wallet, setWallet] = useState<WalletDetails | null>(null);
  const [recipient, setRecipient] = useState('');
  const [amount, setAmount] = useState('');
  const [notes, setNotes] = useState('');
  const [isLoadingWallet, setIsLoadingWallet] = useState(true);

  // Transfer flow states
  const [isConfirmOpen, setIsConfirmOpen] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [errorCode, setErrorCode] = useState<string | null>(null);
  const [successResult, setSuccessResult] = useState<TransferResponse | null>(null);

  useEffect(() => {
    loadWallet();
  }, []);

  const loadWallet = async () => {
    setIsLoadingWallet(true);
    try {
      const data = await walletService.getMyWallet();
      setWallet(data);
    } catch (err: any) {
      setError(err.message || 'Unable to retrieve your wallet balance.');
    } finally {
      setIsLoadingWallet(false);
    }
  };

  const handleInitiateTransfer = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setErrorCode(null);

    const amountNum = parseFloat(amount);
    if (isNaN(amountNum) || amountNum <= 0) {
      setError('Please specify a transfer amount strictly greater than zero.');
      return;
    }

    if (!recipient.trim()) {
      setError('Recipient wallet number or phone number is required.');
      return;
    }

    // Open confirmation step
    setIsConfirmOpen(true);
  };

  const handleExecuteTransfer = async () => {
    const amountNum = parseFloat(amount);
    setIsSubmitting(true);
    setError(null);
    setErrorCode(null);

    try {
      const response = await transferService.transferP2P({
        receiverWalletNumber: recipient.trim(),
        amount: amountNum,
        notes: notes.trim() || undefined,
      });

      setSuccessResult(response);
      setIsConfirmOpen(false);

      // Refresh wallet balance from server (authoritative backend source)
      const updatedWallet = await walletService.getMyWallet();
      setWallet(updatedWallet);
    } catch (err: any) {
      setIsConfirmOpen(false);
      setError(err.message || 'Transfer failed.');
      setErrorCode(err.errorCode || null);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleReset = () => {
    setRecipient('');
    setAmount('');
    setNotes('');
    setError(null);
    setErrorCode(null);
    setSuccessResult(null);
  };

  if (isLoadingWallet) {
    return <LoadingSpinner message="Checking available balance and limits..." size="lg" />;
  }

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Peer-to-Peer Fund Transfer</h1>
        <p className="text-sm text-slate-500 mt-1">Real-time instant transfer across FinPay accounts</p>
      </div>

      {/* Available Balance Banner */}
      <div className="bg-slate-900 text-white p-5 rounded-2xl flex items-center justify-between border border-slate-800 shadow-sm">
        <div>
          <span className="text-xs text-slate-400 font-medium">Your Available Balance</span>
          <p className="text-2xl font-black text-white mt-0.5">
            PKR {wallet?.balance.toLocaleString('en-US', { minimumFractionDigits: 2 })}
          </p>
        </div>

        <div className="text-right">
          <span className="text-xs text-slate-400 font-medium">Daily Limit Remaining</span>
          <p className="text-sm font-bold text-emerald-400 mt-0.5">
            PKR {wallet?.remainingDailyLimit.toLocaleString('en-US', { minimumFractionDigits: 2 })}
          </p>
        </div>
      </div>

      {wallet?.isFrozen && (
        <div className="rounded-xl border border-rose-200 bg-rose-50 p-4 flex items-center space-x-3 text-rose-800">
          <ShieldAlert className="w-5 h-5 text-rose-600 shrink-0" />
          <span className="text-xs font-semibold">
            Your wallet is currently frozen. Outgoing transfers will be rejected by the backend engine.
          </span>
        </div>
      )}

      {/* Success View */}
      {successResult ? (
        <div className="bg-white rounded-2xl shadow-sm border border-emerald-200 p-8 text-center space-y-6 animate-in fade-in zoom-in-95">
          <div className="w-14 h-14 bg-emerald-100 text-emerald-600 rounded-full flex items-center justify-center mx-auto">
            <CheckCircle2 className="w-8 h-8" />
          </div>

          <div>
            <span className="text-xs font-semibold uppercase tracking-wider text-emerald-700 bg-emerald-50 px-2.5 py-1 rounded-full border border-emerald-200">
              Transaction Completed
            </span>
            <h2 className="text-2xl font-black text-slate-900 mt-3">
              PKR {successResult.amount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
            </h2>
            <p className="text-xs text-slate-500 mt-1">
              Transferred to wallet <span className="font-mono font-semibold text-slate-700">{successResult.receiverWalletNumber}</span>
            </p>
          </div>

          {/* Details breakdown */}
          <div className="rounded-xl bg-slate-50 border border-slate-200/80 p-4 text-xs space-y-2 text-left">
            <div className="flex justify-between">
              <span className="text-slate-500">Transaction Reference:</span>
              <span className="font-mono font-semibold text-slate-900">{successResult.referenceId}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-500">Fee Charged:</span>
              <span className="font-semibold text-slate-900">PKR {successResult.fee.toFixed(2)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-500">Net Credited to Recipient:</span>
              <span className="font-semibold text-emerald-600">PKR {successResult.netCreditedAmount.toFixed(2)}</span>
            </div>
            <div className="flex justify-between pt-2 border-t border-slate-200">
              <span className="text-slate-500">Your New Balance:</span>
              <span className="font-bold text-slate-900">PKR {successResult.senderBalanceAfter.toLocaleString('en-US', { minimumFractionDigits: 2 })}</span>
            </div>
          </div>

          <button
            onClick={handleReset}
            className="w-full rounded-xl bg-slate-900 hover:bg-slate-800 text-white py-3 text-xs font-bold transition flex items-center justify-center space-x-2"
          >
            <RotateCcw className="w-4 h-4" />
            <span>Make Another Transfer</span>
          </button>
        </div>
      ) : (
        /* Form View */
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200/80 p-7">
          {error && (
            <div className="mb-6">
              <ErrorMessage message={error} errorCode={errorCode} />
            </div>
          )}

          <form onSubmit={handleInitiateTransfer} className="space-y-5">
            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Recipient Wallet or Phone Number
              </label>
              <div className="relative">
                <UserCheck className="w-4 h-4 text-slate-400 absolute left-3.5 top-3.5" />
                <input
                  type="text"
                  required
                  value={recipient}
                  onChange={(e) => setRecipient(e.target.value)}
                  placeholder="10-digit wallet number or phone"
                  className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-slate-300 text-sm font-medium text-slate-900 placeholder-slate-400 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              </div>
              <p className="text-[11px] text-slate-400 mt-1">
                Enter recipient's 10-digit wallet number (e.g. 7483920184) or registered phone (+92300...).
              </p>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Transfer Amount in PKR
              </label>
              <div className="relative">
                <span className="absolute left-3.5 top-2.5 text-sm font-bold text-slate-400">PKR</span>
                <input
                  type="number"
                  min="0.01"
                  step="0.01"
                  required
                  value={amount}
                  onChange={(e) => setAmount(e.target.value)}
                  placeholder="0.00"
                  className="w-full pl-14 pr-4 py-2.5 rounded-xl border border-slate-300 text-sm font-bold text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                />
              </div>
            </div>

            <div>
              <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                Payment Memo / Note (Optional)
              </label>
              <input
                type="text"
                value={notes}
                onChange={(e) => setNotes(e.target.value)}
                placeholder="e.g. Invoice #1024 or Dinner split"
                className="w-full px-4 py-2 rounded-xl border border-slate-300 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
              />
            </div>

            <button
              type="submit"
              disabled={isSubmitting || wallet?.isFrozen}
              className="w-full mt-4 rounded-xl bg-emerald-600 py-3 text-sm font-semibold text-white hover:bg-emerald-700 disabled:bg-slate-300 transition flex items-center justify-center space-x-2 shadow-sm"
            >
              <Send className="w-4 h-4" />
              <span>Continue to Confirmation</span>
              <ArrowRight className="w-4 h-4" />
            </button>
          </form>
        </div>
      )}

      {/* Confirmation Modal */}
      <ConfirmModal
        isOpen={isConfirmOpen}
        title="Authorize P2P Transfer"
        message="Please verify recipient details and amount. The transfer is atomic and immutable."
        amount={parseFloat(amount) || 0}
        recipient={recipient}
        isSubmitting={isSubmitting}
        onConfirm={handleExecuteTransfer}
        onCancel={() => setIsConfirmOpen(false)}
      />
    </div>
  );
};
