import React, { useState } from 'react';
import { adminService } from '../services/adminService';
import type { WalletDetails } from '../types/wallet';
import { ErrorMessage } from '../components/Common/ErrorMessage';
import { StatusBadge } from '../components/Common/StatusBadge';
import {
  ShieldAlert,
  Lock,
  Unlock,
  AlertTriangle,
  CheckCircle2,
  FileCheck
} from 'lucide-react';

export const AdminPage: React.FC = () => {
  const [walletId, setWalletId] = useState('');
  const [reason, setReason] = useState('');
  const [isFrozenAction, setIsFrozenAction] = useState<boolean>(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [successWallet, setSuccessWallet] = useState<WalletDetails | null>(null);

  const handleAction = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!walletId.trim()) {
      setError('Please provide a valid Wallet GUID.');
      return;
    }

    setIsSubmitting(true);
    setError(null);
    setSuccessWallet(null);

    try {
      const updated = await adminService.freezeWallet(walletId.trim(), {
        isFrozen: isFrozenAction,
        reason: reason.trim() || undefined,
      });
      setSuccessWallet(updated);
      setReason('');
    } catch (err: any) {
      setError(err.message || 'Administrative action failed.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="space-y-8 max-w-4xl">
      <div>
        <div className="flex items-center space-x-2.5 mb-1">
          <div className="p-2 rounded-xl bg-rose-100 text-rose-700">
            <ShieldAlert className="w-5 h-5" />
          </div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Compliance & Administration Console</h1>
        </div>
        <p className="text-sm text-slate-500">
          Supervisory controls to freeze or unfreeze accounts flagged for suspicious velocity or compliance review
        </p>
      </div>

      {/* Warning Notice */}
      <div className="rounded-2xl border border-amber-200 bg-amber-50/80 p-5 flex items-start space-x-3.5 text-amber-900">
        <AlertTriangle className="w-5 h-5 text-amber-600 mt-0.5 shrink-0" />
        <div className="text-xs space-y-1">
          <p className="font-bold">Administrative Role Authorization Active</p>
          <p className="text-amber-800">
            Actions performed on this console trigger database transaction locks. Freezing a wallet immediately blocks all
            subsequent outgoing P2P transfers and incoming credits across all channels.
          </p>
        </div>
      </div>

      {error && <ErrorMessage message={error} />}

      {/* Freeze / Unfreeze Control Form */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200/80 p-7">
        <h3 className="text-base font-bold text-slate-900 mb-1">Target Account Action</h3>
        <p className="text-xs text-slate-500 mb-6">Enter target wallet ID to toggle compliance freeze status</p>

        <form onSubmit={handleAction} className="space-y-5">
          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Target Wallet GUID
            </label>
            <input
              type="text"
              required
              value={walletId}
              onChange={(e) => setWalletId(e.target.value)}
              placeholder="e.g. 3fa85f64-5717-4562-b3fc-2c963f66afa6"
              className="w-full font-mono px-4 py-2.5 rounded-xl border border-slate-300 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-rose-500"
            />
            <p className="text-[11px] text-slate-400 mt-1">
              You can find target wallet GUIDs in user profiles or audit logs.
            </p>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Desired State
            </label>
            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={() => setIsFrozenAction(true)}
                className={`p-3 rounded-xl border text-xs font-bold flex items-center justify-center space-x-2 transition ${
                  isFrozenAction
                    ? 'bg-rose-50 border-rose-500 text-rose-800 ring-1 ring-rose-500'
                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <Lock className="w-4 h-4 text-rose-600" />
                <span>Freeze Account (Block Transfers)</span>
              </button>

              <button
                type="button"
                onClick={() => setIsFrozenAction(false)}
                className={`p-3 rounded-xl border text-xs font-bold flex items-center justify-center space-x-2 transition ${
                  !isFrozenAction
                    ? 'bg-emerald-50 border-emerald-500 text-emerald-800 ring-1 ring-emerald-500'
                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                }`}
              >
                <Unlock className="w-4 h-4 text-emerald-600" />
                <span>Unfreeze Account (Restore)</span>
              </button>
            </div>
          </div>

          <div>
            <label className="block text-xs font-semibold text-slate-700 uppercase tracking-wider mb-1.5">
              Audit Reason / Case ID
            </label>
            <input
              type="text"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="e.g. KYC tier review, Suspicious velocity report #8492"
              className="w-full px-4 py-2.5 rounded-xl border border-slate-300 text-sm text-slate-900 focus:outline-none focus:ring-2 focus:ring-rose-500"
            />
          </div>

          <button
            type="submit"
            disabled={isSubmitting}
            className={`w-full rounded-xl py-3 text-sm font-bold text-white transition flex items-center justify-center space-x-2 shadow-sm ${
              isFrozenAction
                ? 'bg-rose-600 hover:bg-rose-700 disabled:bg-rose-400'
                : 'bg-emerald-600 hover:bg-emerald-700 disabled:bg-emerald-400'
            }`}
          >
            {isSubmitting ? (
              <span>Executing Compliance State Change...</span>
            ) : isFrozenAction ? (
              <>
                <Lock className="w-4 h-4" />
                <span>Execute Account Freeze</span>
              </>
            ) : (
              <>
                <Unlock className="w-4 h-4" />
                <span>Execute Account Unfreeze</span>
              </>
            )}
          </button>
        </form>
      </div>

      {/* Result Card */}
      {successWallet && (
        <div className="bg-white rounded-2xl shadow-sm border border-slate-200/80 p-6 space-y-4 animate-in fade-in">
          <div className="flex items-center space-x-3 text-emerald-700">
            <CheckCircle2 className="w-6 h-6 shrink-0" />
            <div>
              <h4 className="text-sm font-bold">Compliance Status Updated</h4>
              <p className="text-xs text-slate-500">The state has been committed to SQL Server.</p>
            </div>
          </div>

          <div className="rounded-xl bg-slate-50 p-4 text-xs space-y-2 border border-slate-200/80">
            <div className="flex justify-between">
              <span className="text-slate-500">Wallet Number:</span>
              <span className="font-mono font-bold text-slate-900">{successWallet.walletNumber}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-500">Balance Protected:</span>
              <span className="font-bold text-slate-900">
                PKR {successWallet.balance.toLocaleString('en-US', { minimumFractionDigits: 2 })}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-500">Operational Status:</span>
              <StatusBadge status={successWallet.isFrozen ? 'Frozen' : 'Active'} />
            </div>
          </div>
        </div>
      )}

      {/* Compliance Policy Guidelines */}
      <div className="bg-slate-900 text-slate-300 rounded-2xl p-6 border border-slate-800 text-xs space-y-2">
        <div className="flex items-center space-x-2 text-white font-bold mb-2">
          <FileCheck className="w-4 h-4 text-emerald-400" />
          <span>Supervisory Regulatory Guidelines</span>
        </div>
        <p>• Tier 1 Basic Wallets are subject to PKR 50,000 maximum daily transfer limits.</p>
        <p>• Tier 2 Merchant Accounts are subject to PKR 1,000,000 daily limits with 0.5% incoming service tax.</p>
        <p>• All transactions are logged to immutable double-entry ledger audit records.</p>
      </div>
    </div>
  );
};
