import React, { useState, useEffect } from 'react';
import { transactionService } from '../services/transactionService';
import type { TransactionItem } from '../types/transaction';
import type { PagedResult } from '../types/api';
import { LoadingSpinner } from '../components/Common/LoadingSpinner';
import { ErrorMessage } from '../components/Common/ErrorMessage';
import { StatusBadge } from '../components/Common/StatusBadge';
import {
  ArrowUpRight,
  ArrowDownLeft,
  ChevronLeft,
  ChevronRight,
  Filter,
  RefreshCw,
  Wallet
} from 'lucide-react';

export const TransactionsPage: React.FC = () => {
  const [data, setData] = useState<PagedResult<TransactionItem> | null>(null);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [typeFilter, setTypeFilter] = useState<number | undefined>(undefined);
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchHistory = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const result = await transactionService.getHistory({
        pageNumber,
        pageSize,
        startDate: startDate ? new Date(startDate).toISOString() : undefined,
        endDate: endDate ? new Date(endDate).toISOString() : undefined,
        type: typeFilter,
      });
      setData(result);
    } catch (err: any) {
      setError(err.message || 'Failed to retrieve statement history.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchHistory();
  }, [pageNumber, pageSize, typeFilter]);

  const handleApplyFilters = (e: React.FormEvent) => {
    e.preventDefault();
    setPageNumber(1);
    fetchHistory();
  };

  const handleResetFilters = () => {
    setStartDate('');
    setEndDate('');
    setTypeFilter(undefined);
    setPageNumber(1);
  };

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-slate-900 tracking-tight">Statement & Ledger History</h1>
          <p className="text-sm text-slate-500 mt-1">Audit log of all credits, debits, and balance transitions</p>
        </div>

        <button
          onClick={fetchHistory}
          disabled={isLoading}
          className="self-start sm:self-auto rounded-xl border border-slate-300 bg-white hover:bg-slate-50 text-slate-700 px-3.5 py-2 text-xs font-semibold transition flex items-center space-x-2"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`} />
          <span>Refresh</span>
        </button>
      </div>

      {/* Filters Bar */}
      <div className="bg-white rounded-2xl p-5 border border-slate-200/80 shadow-xs">
        <form onSubmit={handleApplyFilters} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 items-end">
          <div>
            <label className="block text-[11px] font-bold uppercase text-slate-500 mb-1">Transaction Type</label>
            <select
              value={typeFilter ?? ''}
              onChange={(e) => setTypeFilter(e.target.value ? parseInt(e.target.value) : undefined)}
              className="w-full text-xs font-semibold rounded-xl border border-slate-300 py-2 px-3 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            >
              <option value="">All Types</option>
              <option value="1">P2P Transfers</option>
              <option value="2">Deposits</option>
            </select>
          </div>

          <div>
            <label className="block text-[11px] font-bold uppercase text-slate-500 mb-1">From Date</label>
            <input
              type="date"
              value={startDate}
              onChange={(e) => setStartDate(e.target.value)}
              className="w-full text-xs font-medium rounded-xl border border-slate-300 py-2 px-3 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div>
            <label className="block text-[11px] font-bold uppercase text-slate-500 mb-1">To Date</label>
            <input
              type="date"
              value={endDate}
              onChange={(e) => setEndDate(e.target.value)}
              className="w-full text-xs font-medium rounded-xl border border-slate-300 py-2 px-3 focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
          </div>

          <div className="flex space-x-2">
            <button
              type="submit"
              className="flex-1 rounded-xl bg-slate-900 hover:bg-slate-800 text-white py-2 px-3 text-xs font-bold transition flex items-center justify-center space-x-1.5"
            >
              <Filter className="w-3.5 h-3.5" />
              <span>Filter</span>
            </button>
            <button
              type="button"
              onClick={handleResetFilters}
              className="rounded-xl border border-slate-300 hover:bg-slate-50 text-slate-600 py-2 px-3 text-xs font-semibold transition"
            >
              Reset
            </button>
          </div>
        </form>
      </div>

      {error && <ErrorMessage message={error} onRetry={fetchHistory} />}

      {/* Transactions Table */}
      <div className="bg-white rounded-2xl shadow-sm border border-slate-200/80 overflow-hidden">
        {isLoading ? (
          <div className="py-16">
            <LoadingSpinner message="Querying paginated ledger records..." size="md" />
          </div>
        ) : !data || data.items.length === 0 ? (
          <div className="p-12 text-center text-slate-400">
            <Wallet className="w-10 h-10 mx-auto text-slate-300 mb-3" />
            <p className="text-sm font-medium text-slate-600">No transactions match your criteria</p>
            <p className="text-xs text-slate-400 mt-1">Try resetting the filters or modifying the date range.</p>
          </div>
        ) : (
          <>
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm text-slate-600">
                <thead className="bg-slate-50 text-[11px] font-bold uppercase tracking-wider text-slate-500 border-b border-slate-100">
                  <tr>
                    <th className="py-3 px-6">Reference ID</th>
                    <th className="py-3 px-6">Counterparty</th>
                    <th className="py-3 px-6">Type</th>
                    <th className="py-3 px-6">Status</th>
                    <th className="py-3 px-6">Timestamp</th>
                    <th className="py-3 px-6 text-right">Balance After</th>
                    <th className="py-3 px-6 text-right">Amount</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 text-xs font-medium">
                  {data.items.map((tx) => {
                    const isDebit = tx.direction === 'Debit';
                    return (
                      <tr key={tx.id} className="hover:bg-slate-50/70 transition">
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
                              {isDebit ? <ArrowUpRight className="w-3.5 h-3.5" /> : <ArrowDownLeft className="w-3.5 h-3.5" />}
                            </div>
                            <span className="font-mono text-slate-800">
                              {isDebit ? tx.receiverWalletNumber : (tx.senderWalletNumber || 'System')}
                            </span>
                          </div>
                        </td>
                        <td className="py-3.5 px-6">{tx.type}</td>
                        <td className="py-3.5 px-6">
                          <StatusBadge status={tx.status} />
                        </td>
                        <td className="py-3.5 px-6 text-slate-400 font-mono text-[11px]">
                          {new Date(tx.createdAt).toLocaleDateString()} {new Date(tx.createdAt).toLocaleTimeString()}
                        </td>
                        <td className="py-3.5 px-6 text-right font-mono text-slate-700">
                          {tx.balanceAfter !== null && tx.balanceAfter !== undefined
                            ? `PKR ${tx.balanceAfter.toLocaleString('en-US', { minimumFractionDigits: 2 })}`
                            : '-'}
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

            {/* Pagination Controls */}
            <div className="p-4 border-t border-slate-100 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500">
              <div className="flex items-center space-x-3">
                <span>
                  Showing Page <strong className="text-slate-800">{data.pageNumber}</strong> of{' '}
                  <strong className="text-slate-800">{data.totalPages || 1}</strong> ({data.totalCount} total)
                </span>
                <select
                  value={pageSize}
                  onChange={(e) => {
                    setPageSize(parseInt(e.target.value));
                    setPageNumber(1);
                  }}
                  className="rounded-lg border border-slate-300 py-1 px-2 text-xs text-slate-700 bg-white"
                >
                  <option value="5">5 / page</option>
                  <option value="10">10 / page</option>
                  <option value="20">20 / page</option>
                  <option value="50">50 / page</option>
                </select>
              </div>

              <div className="flex items-center space-x-2">
                <button
                  disabled={!data.hasPreviousPage}
                  onClick={() => setPageNumber((p) => Math.max(1, p - 1))}
                  className="rounded-lg border border-slate-300 px-3 py-1.5 font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition flex items-center space-x-1"
                >
                  <ChevronLeft className="w-3.5 h-3.5" />
                  <span>Previous</span>
                </button>
                <button
                  disabled={!data.hasNextPage}
                  onClick={() => setPageNumber((p) => p + 1)}
                  className="rounded-lg border border-slate-300 px-3 py-1.5 font-semibold text-slate-700 hover:bg-slate-50 disabled:opacity-40 disabled:cursor-not-allowed transition flex items-center space-x-1"
                >
                  <span>Next</span>
                  <ChevronRight className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
};
