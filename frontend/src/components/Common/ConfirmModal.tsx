import React from 'react';
import { ShieldCheck, AlertTriangle } from 'lucide-react';

interface ConfirmModalProps {
  isOpen: boolean;
  title: string;
  message: string;
  amount?: number;
  recipient?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  isSubmitting?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export const ConfirmModal: React.FC<ConfirmModalProps> = ({
  isOpen,
  title,
  message,
  amount,
  recipient,
  confirmLabel = 'Confirm Transfer',
  cancelLabel = 'Cancel',
  isSubmitting = false,
  onConfirm,
  onCancel,
}) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-slate-900/50 backdrop-blur-xs p-4">
      <div className="w-full max-w-md rounded-2xl bg-white shadow-2xl border border-slate-100 overflow-hidden animate-in fade-in zoom-in-95 duration-150">
        <div className="p-6">
          <div className="flex items-center space-x-3 mb-4">
            <div className="flex h-10 w-10 items-center justify-center rounded-full bg-emerald-100 text-emerald-700">
              <ShieldCheck className="h-6 w-6" />
            </div>
            <h3 className="text-lg font-bold text-slate-900">{title}</h3>
          </div>

          <p className="text-sm text-slate-600 mb-4">{message}</p>

          {amount !== undefined && (
            <div className="rounded-xl bg-slate-50 border border-slate-200/80 p-4 mb-4">
              <div className="flex justify-between items-center text-sm mb-1.5">
                <span className="text-slate-500 font-medium">Transfer Amount:</span>
                <span className="text-base font-bold text-slate-900">
                  PKR {amount.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </span>
              </div>
              {recipient && (
                <div className="flex justify-between items-center text-sm">
                  <span className="text-slate-500 font-medium">Recipient:</span>
                  <span className="font-mono font-semibold text-slate-800">{recipient}</span>
                </div>
              )}
            </div>
          )}

          <div className="flex items-start text-xs text-amber-700 bg-amber-50 rounded-lg p-2.5 mb-6 border border-amber-200">
            <AlertTriangle className="w-4 h-4 mr-2 shrink-0 mt-0.5" />
            <span>
              Once confirmed, this transaction cannot be reversed. Funds will be deducted from your wallet immediately.
            </span>
          </div>

          <div className="flex space-x-3">
            <button
              type="button"
              disabled={isSubmitting}
              onClick={onCancel}
              className="flex-1 rounded-xl border border-slate-300 py-2.5 text-sm font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-50 transition"
            >
              {cancelLabel}
            </button>
            <button
              type="button"
              disabled={isSubmitting}
              onClick={onConfirm}
              className="flex-1 rounded-xl bg-emerald-600 py-2.5 text-sm font-semibold text-white hover:bg-emerald-700 disabled:bg-emerald-400 transition flex items-center justify-center space-x-2 shadow-sm"
            >
              {isSubmitting ? (
                <>
                  <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                  <span>Processing...</span>
                </>
              ) : (
                <span>{confirmLabel}</span>
              )}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
