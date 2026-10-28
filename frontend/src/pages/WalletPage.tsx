import React, { useState, useEffect } from 'react';
import { walletService } from '../services/walletService';
import type { WalletDetails } from '../types/wallet';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { ErrorMessage } from '../components/Common/ErrorMessage';
import { StatusBadge } from '../components/Common/StatusBadge';
import {
  Wallet,
  ShieldCheck,
  CreditCard,
  Lock,
  Layers,
  Percent,
  PlusCircle
} from 'lucide-react';

export const WalletPage: React.FC = () => {
  const [wallet, setWallet] = useState<WalletDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Top-up modal
  const [isDepositOpen, setIsDepositOpen] = useState(false);
  const [depositAmount, setDepositAmount] = useState('5000');
  const [isDepositing, setIsDepositing] = useState(false);

  const loadWallet = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await walletService.getMyWallet();
      setWallet(data);
    } catch (err: any) {
      setError(err.message || 'Failed to retrieve wallet information.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadWallet();
  }, []);

  const handleDeposit = async (e: React.FormEvent) => {
    e.preventDefault();
    const amount = parseFloat(depositAmount);
    if (isNaN(amount) || amount <= 0) return;

    setIsDepositing(true);
    try {
      const updated = await walletService.deposit({ amount, notes: 'Wallet details top-up' });
      setWallet(updated);
      setIsDepositOpen(false);
    } catch (err: any) {
      alert(err.message || 'Top-up failed');
    } finally {
      setIsDepositing(false);
    }
  };

  if (isLoading) {
    return <LoadingSpinner message="Querying wallet state..." size="lg" />;
  }

  if (error) {
    return <ErrorMessage message={error} onRetry={loadWallet} />;
  }

  const isMerchant = wallet?.walletType === 2 || wallet?.walletType === 'Merchant';

  return (
    <div className="space-y-8 max-w-5xl">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Wallet Specification & Details</h1>
          <p className="text-sm text-slate-500 mt-1">Encapsulation and Tier Management Details</p>
        </div>
        <button
          onClick={() => setIsDepositOpen(true)}
          className="rounded-xl bg-slate-900 hover:bg-slate-800 text-white px-4 py-2.5 text-xs font-bold transition flex items-center space-x-2 shadow-sm"
        >
          <PlusCircle className="w-4 h-4 text-emerald-400" />
          <span>Top-Up Balance</span>
        </button>
      </div>

      {/* Main Stats Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-5">
        <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center space-x-2 text-slate-500 text-xs font-semibold mb-2">
            <CreditCard className="w-4 h-4 text-emerald-600" />
            <span>Wallet Number</span>
          </div>
          <p className="font-mono text-xl font-bold text-slate-900 tracking-wider">{wallet?.walletNumber}</p>
          <p className="text-[11px] text-slate-400 mt-1">10-Digit Unique Account Identifier</p>
        </div>

        <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center space-x-2 text-slate-500 text-xs font-semibold mb-2">
            <Wallet className="w-4 h-4 text-emerald-600" />
            <span>Available Balance</span>
          </div>
          <p className="text-xl font-bold text-emerald-600">
            PKR {wallet?.balance.toLocaleString('en-US', { minimumFractionDigits: 2 })}
          </p>
          <p className="text-[11px] text-slate-400 mt-1">Zero-Negative Invariant Protected</p>
        </div>

        <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center space-x-2 text-slate-500 text-xs font-semibold mb-2">
            <Layers className="w-4 h-4 text-emerald-600" />
            <span>Account Tier</span>
          </div>
          <div className="flex items-center space-x-2">
            <span className="font-bold text-slate-900">{isMerchant ? 'Merchant (Level 2)' : 'Basic (Level 1)'}</span>
          </div>
          <p className="text-[11px] text-slate-400 mt-1">
            {isMerchant ? 'Limit: PKR 1,000,000 / day' : 'Limit: PKR 50,000 / day'}
          </p>
        </div>

        <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center space-x-2 text-slate-500 text-xs font-semibold mb-2">
            <Lock className="w-4 h-4 text-emerald-600" />
            <span>Account Status</span>
          </div>
          <StatusBadge status={wallet?.isFrozen ? 'Frozen' : 'Active'} />
          <p className="text-[11px] text-slate-400 mt-1">
            {wallet?.isFrozen ? 'All transfers blocked' : 'Operating normally'}
          </p>
        </div>
      </div>

      {/* Fee Schedule & Technical Architecture Breakdown */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Tier Fee Policy */}
        <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center space-x-2.5 mb-4">
            <div className="p-2 rounded-xl bg-emerald-50 text-emerald-700">
              <Percent className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-slate-900">Fee Schedule & Polymorphism</h3>
              <p className="text-xs text-slate-500">Configured rule sets for your tier</p>
            </div>
          </div>

          <div className="space-y-3 text-xs">
            <div className="flex justify-between p-3 rounded-xl bg-slate-50 border border-slate-100">
              <span className="text-slate-600 font-medium">P2P Transfers (Outgoing):</span>
              <span className="font-bold text-emerald-700">PKR 0.00 (Free)</span>
            </div>

            <div className="flex justify-between p-3 rounded-xl bg-slate-50 border border-slate-100">
              <span className="text-slate-600 font-medium">Incoming Service Fee:</span>
              <span className="font-bold text-slate-900">
                {isMerchant ? '0.5% (Deducted from credit)' : '0.0%'}
              </span>
            </div>

            <div className="flex justify-between p-3 rounded-xl bg-slate-50 border border-slate-100">
              <span className="text-slate-600 font-medium">ATM / Bank Withdrawal Fee:</span>
              <span className="font-bold text-slate-900">1.5%</span>
            </div>
          </div>
        </div>

        {/* Technical Controls & Guarantees */}
        <div className="bg-white rounded-2xl p-6 border border-slate-200/80 shadow-xs">
          <div className="flex items-center space-x-2.5 mb-4">
            <div className="p-2 rounded-xl bg-slate-100 text-slate-800">
              <ShieldCheck className="w-5 h-5" />
            </div>
            <div>
              <h3 className="text-sm font-bold text-slate-900">Financial Integrity Guarantees</h3>
              <p className="text-xs text-slate-500">Core engine architecture rules</p>
            </div>
          </div>

          <ul className="space-y-2.5 text-xs text-slate-600">
            <li className="flex items-start space-x-2">
              <span className="text-emerald-500 font-bold">&check;</span>
              <span><strong>Encapsulation:</strong> Balance cannot be modified directly; all updates run through domain operations.</span>
            </li>
            <li className="flex items-start space-x-2">
              <span className="text-emerald-500 font-bold">&check;</span>
              <span><strong>Optimistic Concurrency:</strong> SQL Server rowversion prevents simultaneous overlapping debit conflicts.</span>
            </li>
            <li className="flex items-start space-x-2">
              <span className="text-emerald-500 font-bold">&check;</span>
              <span><strong>Double-Entry Ledger:</strong> Transfers atomically append balanced Debit and Credit rows.</span>
            </li>
          </ul>
        </div>
      </div>

      {/* Top-up Modal */}
      {isDepositOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4">
          <div className="w-full max-w-md rounded-2xl bg-white shadow-2xl border border-slate-100 p-6">
            <h3 className="text-lg font-bold text-slate-900 mb-1">Add Testing Funds</h3>
            <p className="text-xs text-slate-500 mb-6">Credits simulated funds into your wallet.</p>

            <form onSubmit={handleDeposit} className="space-y-4">
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
                  className="flex-1 rounded-xl bg-emerald-600 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 transition"
                >
                  {isDepositing ? 'Processing...' : 'Confirm'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};
