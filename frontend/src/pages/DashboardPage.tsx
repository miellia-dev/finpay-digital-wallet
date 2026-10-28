import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { walletService } from '../services/walletService';
import { transactionService } from '../services/transactionService';
import type { WalletDetails } from '../types/wallet';
import type { TransactionItem } from '../types/transaction';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { ErrorMessage } from '../components/Common/ErrorMessage';
import { StatusBadge } from '../components/Common/StatusBadge';
import {
  Wallet,
  ArrowUpRight,
  ArrowDownLeft,
  PlusCircle,
  Clock,
  ShieldCheck,
  Copy,
  Check,
  Send
} from 'lucide-react';

export const DashboardPage: React.FC = () => {
  const [wallet, setWallet] = useState<WalletDetails | null>(null);
  const [recentTransactions, setRecentTransactions] = useState<TransactionItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [copied, setCopied] = useState(false);

  // Top-up modal state
  const [isDepositOpen, setIsDepositOpen] = useState(false);
  const [depositAmount, setDepositAmount] = useState('10000');
  const [isDepositing, setIsDepositing] = useState(false);
  const [depositError, setDepositError] = useState<string | null>(null);

  const loadData = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const [walletData, historyData] = await Promise.all([
        walletService.getMyWallet(),
        transactionService.getHistory({ pageNumber: 1, pageSize: 5 }),
      ]);
      setWallet(walletData);
      setRecentTransactions(historyData.items);
    } catch (err: any) {
      setError(err.message || 'Failed to fetch dashboard data.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, []);

  const handleCopy = () => {
    if (wallet?.walletNumber) {
      navigator.clipboard.writeText(wallet.walletNumber);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  const handleDepositSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const amountNum = parseFloat(depositAmount);
    if (isNaN(amountNum) || amountNum <= 0) {
      setDepositError('Please enter a valid amount greater than 0.');
      return;
    }

    setIsDepositing(true);
    setDepositError(null);
    try {
      const updated = await walletService.deposit({ amount: amountNum, notes: 'Dashboard top-up' });
      setWallet(updated);
      setIsDepositOpen(false);
      // Refresh transactions
      const historyData = await transactionService.getHistory({ pageNumber: 1, pageSize: 5 });
      setRecentTransactions(historyData.items);
    } catch (err: any) {
      setDepositError(err.message || 'Deposit failed.');
    } finally {
      setIsDepositing(false);
    }
  };

  if (isLoading) {
    return <LoadingSpinner message="Retrieving real-time wallet balance..." size="lg" />;
  }

  if (error) {
    return <ErrorMessage message={error} onRetry={loadData} />;
  }

  const dailyUsagePercent = wallet
    ? Math.min(100, Math.round((wallet.transferredToday / wallet.dailyTransferLimit) * 100))
    : 0;

  return (
    <div className="space-y-8">
      {/* Page Title */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Financial Overview</h1>
        <p className="text-sm text-slate-500 mt-1">Real-time balances and ledger-backed operations</p>
      </div>

      {/* Top Cards Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Balance Card */}
        <div className="lg:col-span-2 rounded-2xl bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 text-white p-7 shadow-xl border border-slate-700/50 flex flex-col justify-between relative overflow-hidden">
          <div className="absolute right-0 top-0 -mt-8 -mr-8 w-48 h-48 rounded-full bg-emerald-500/10 blur-3xl pointer-events-none" />

          <div>
            <div className="flex justify-between items-start mb-6">
              <div>
                <span className="text-xs font-semibold text-emerald-400 uppercase tracking-widest bg-emerald-950/80 px-2.5 py-1 rounded-md border border-emerald-500/30">
                  {wallet?.walletType === 2 || wallet?.walletType === 'Merchant'
                    ? 'Level 2: Merchant Tier'
                    : 'Level 1: Basic Tier'}
                </span>
                <p className="text-xs text-slate-400 mt-2">Available Balance</p>
              </div>

              {/* Wallet Number Pill */}
              <button
                onClick={handleCopy}
                className="flex items-center space-x-2 bg-slate-800/80 hover:bg-slate-700/80 border border-slate-700 px-3 py-1.5 rounded-xl text-xs font-mono text-slate-300 transition"
                title="Click to copy wallet number"
              >
                <span>No: {wallet?.walletNumber}</span>
                {copied ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5 text-slate-400" />}
              </button>
            </div>

            {/* Big Balance Number */}
            <div className="flex items-baseline space-x-2 my-2">
              <span className="text-xl font-bold text-slate-400">PKR</span>
              <span className="text-4xl lg:text-5xl font-black tracking-tight text-white">
                {wallet?.balance.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </span>
            </div>

            {wallet?.isFrozen && (
              <div className="inline-flex items-center space-x-2 bg-rose-950/80 border border-rose-500/40 text-rose-300 px-3 py-1 rounded-lg text-xs mt-3">
                <span className="w-2 h-2 rounded-full bg-rose-500 animate-pulse" />
                <span>Wallet is Frozen by Compliance. Transactions restricted.</span>
              </div>
            )}
          </div>

          {/* Quick Action Buttons */}
          <div className="flex items-center space-x-3 pt-6 mt-6 border-t border-slate-800">
            <Link
              to="/send"
              className="flex-1 rounded-xl bg-emerald-600 hover:bg-emerald-500 py-3 px-4 text-xs font-bold text-white transition flex items-center justify-center space-x-2 shadow-md shadow-emerald-900/40"
            >
              <Send className="w-4 h-4" />
              <span>Send Funds (P2P)</span>
            </Link>

            <button
              onClick={() => setIsDepositOpen(true)}
              className="flex-1 rounded-xl bg-slate-800 hover:bg-slate-700 border border-slate-700 py-3 px-4 text-xs font-bold text-slate-200 transition flex items-center justify-center space-x-2"
            >
              <PlusCircle className="w-4 h-4 text-emerald-400" />
              <span>Top-Up (Simulated)</span>
            </button>
          </div>
        </div>

        {/* Daily Velocity Limit Card */}
        <div className="rounded-2xl bg-white p-7 shadow-sm border border-slate-200/80 flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-sm font-bold text-slate-900">Daily Transfer Velocity</h3>
              <Clock className="w-4 h-4 text-slate-400" />
            </div>

            <p className="text-xs text-slate-500 mb-4">
              Server-enforced 24-hour UTC window transfer limit.
            </p>

            <div className="space-y-3">
              <div>
                <div className="flex justify-between text-xs font-semibold mb-1">
                  <span className="text-slate-500">Transferred Today:</span>
                  <span className="text-slate-900">
                    PKR {wallet?.transferredToday.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                  </span>
                </div>
                <div className="w-full bg-slate-100 rounded-full h-2.5 overflow-hidden">
                  <div
                    className={`h-2.5 rounded-full transition-all duration-500 ${
                      dailyUsagePercent > 85 ? 'bg-rose-500' : 'bg-emerald-500'
                    }`}
                    style={{ width: `${dailyUsagePercent}%` }}
                  />
                </div>
              </div>

              <div className="flex justify-between text-xs py-2 border-t border-slate-100">
                <span className="text-slate-500 font-medium">Daily Limit:</span>
                <span className="font-bold text-slate-900">
                  PKR {wallet?.dailyTransferLimit.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                </span>
              </div>

              <div className="flex justify-between text-xs py-2 border-t border-slate-100">
                <span className="text-slate-500 font-medium">Remaining Capacity:</span>
                <span className="font-bold text-emerald-600">
                  PKR {wallet?.remainingDailyLimit.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                </span>
              </div>
            </div>
          </div>

          <div className="pt-4 mt-4 border-t border-slate-100 text-[11px] text-slate-400 flex items-center space-x-1.5">
            <ShieldCheck className="w-3.5 h-3.5 text-slate-500 shrink-0" />
            <span>Concurrency locks prevent race conditions exceeding this quota.</span>
          </div>
        </div>
      </div>

      {/* Recent Transactions Section */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200/80 overflow-hidden">
        <div className="p-6 border-b border-slate-100 flex items-center justify-between">
          <div>
            <h3 className="text-base font-bold text-slate-900">Recent Statement Activity</h3>
            <p className="text-xs text-slate-500 mt-0.5">Immutable audit trail entries</p>
          </div>
          <Link
            to="/transactions"
            className="text-xs font-bold text-emerald-600 hover:text-emerald-700 hover:underline"
          >
            View Full Statements &rarr;
          </Link>
        </div>

        {recentTransactions.length === 0 ? (
          <div className="p-12 text-center text-slate-400">
            <Wallet className="w-10 h-10 mx-auto text-slate-300 mb-3" />
            <p className="text-sm font-medium text-slate-600">No transactions recorded yet</p>
            <p className="text-xs text-slate-400 mt-1">Make a top-up or transfer funds to see ledger entries.</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50/75 text-[11px] font-bold uppercase tracking-wider text-slate-500 border-b border-slate-100">
                <tr>
                  <th className="py-3 px-6">Reference</th>
                  <th className="py-3 px-6">Party / Direction</th>
                  <th className="py-3 px-6">Type</th>
                  <th className="py-3 px-6">Status</th>
                  <th className="py-3 px-6">Date</th>
                  <th className="py-3 px-6 text-right">Amount</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 text-xs font-medium">
                {recentTransactions.map((tx) => {
                  const isDebit = tx.direction === 'Debit';
                  return (
                    <tr key={tx.id} className="hover:bg-slate-50/60 transition">
                      <td className="py-3.5 px-6 font-mono text-[11px] text-slate-900">
                        {tx.referenceId}
                      </td>
                      <td className="py-3.5 px-6">
                        <div className="flex items-center space-x-2">
                          <div
                            className={`w-6 h-6 rounded-full flex items-center justify-center shrink-0 ${
                              isDebit ? 'bg-rose-100 text-rose-600' : 'bg-emerald-100 text-emerald-600'
                            }`}
                          >
                            {isDebit ? (
                              <ArrowUpRight className="w-3.5 h-3.5" />
                            ) : (
                              <ArrowDownLeft className="w-3.5 h-3.5" />
                            )}
                          </div>
                          <span className="font-mono text-slate-700">
                            {isDebit ? tx.receiverWalletNumber : (tx.senderWalletNumber || 'System Deposit')}
                          </span>
                        </div>
                      </td>
                      <td className="py-3.5 px-6 text-slate-600">{tx.type}</td>
                      <td className="py-3.5 px-6">
                        <StatusBadge status={tx.status} />
                      </td>
                      <td className="py-3.5 px-6 text-slate-400">
                        {new Date(tx.createdAt).toLocaleDateString()} {new Date(tx.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </td>
                      <td className={`py-3.5 px-6 text-right font-bold ${isDebit ? 'text-rose-600' : 'text-emerald-600'}`}>
                        {isDebit ? '-' : '+'} PKR {tx.amount.toLocaleString('en-US', { minimumFractionDigits: 2 })}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Simulated Deposit Modal */}
      {isDepositOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4">
          <div className="w-full max-w-md rounded-2xl bg-white shadow-2xl border border-slate-100 p-6">
            <h3 className="text-lg font-bold text-slate-900 mb-1">Simulated Top-Up (Deposit)</h3>
            <p className="text-xs text-slate-500 mb-6">
              Development endpoint to credit testing funds into your digital wallet.
            </p>

            {depositError && (
              <div className="mb-4">
                <ErrorMessage message={depositError} />
              </div>
            )}

            <form onSubmit={handleDepositSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
                  Amount in PKR
                </label>
                <div className="relative">
                  <span className="absolute left-3.5 top-2.5 text-sm font-bold text-slate-400">PKR</span>
                  <input
                    type="number"
                    min="1"
                    step="0.01"
                    required
                    value={depositAmount}
                    onChange={(e) => setDepositAmount(e.target.value)}
                    className="w-full pl-14 pr-4 py-2.5 rounded-xl border border-slate-300 text-sm font-bold text-slate-900 focus:outline-none focus:ring-2 focus:ring-emerald-500"
                  />
                </div>
              </div>

              <div className="flex space-x-3 pt-4">
                <button
                  type="button"
                  disabled={isDepositing}
                  onClick={() => setIsDepositOpen(false)}
                  className="flex-1 rounded-xl border border-slate-300 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 transition"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isDepositing}
                  className="flex-1 rounded-xl bg-emerald-600 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 transition flex items-center justify-center space-x-2"
                >
                  {isDepositing ? <span>Processing...</span> : <span>Confirm Deposit</span>}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
