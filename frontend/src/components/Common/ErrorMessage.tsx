import React from 'react';
import { AlertCircle } from 'lucide-react';

interface ErrorMessageProps {
  message: string;
  errorCode?: string | null;
  onRetry?: () => void;
}

export const ErrorMessage: React.FC<ErrorMessageProps> = ({ message, errorCode, onRetry }) => {
  return (
    <div className="rounded-lg border border-rose-200 bg-rose-50 p-4 text-rose-800">
      <div className="flex items-start">
        <AlertCircle className="w-5 h-5 text-rose-600 mt-0.5 shrink-0" />
        <div className="ml-3 flex-1">
          <h4 className="text-sm font-semibold text-rose-900">
            {errorCode ? `Error: ${errorCode}` : 'Transaction Error'}
          </h4>
          <p className="mt-1 text-sm text-rose-700">{message}</p>
          {onRetry && (
            <button
              onClick={onRetry}
              className="mt-3 text-xs font-semibold uppercase tracking-wider text-rose-800 underline hover:text-rose-900"
            >
              Retry Action
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
