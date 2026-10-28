import { apiRequest } from './api';
import type { PagedResult } from '../types/api';
import type { TransactionHistoryQuery, TransactionItem } from '../types/transaction';

export const transactionService = {
  async getHistory(query: TransactionHistoryQuery = {}): Promise<PagedResult<TransactionItem>> {
    const params = new URLSearchParams();
    if (query.pageNumber) params.append('pageNumber', query.pageNumber.toString());
    if (query.pageSize) params.append('pageSize', query.pageSize.toString());
    if (query.startDate) params.append('startDate', query.startDate);
    if (query.endDate) params.append('endDate', query.endDate);
    if (query.type) params.append('type', query.type.toString());

    const qs = params.toString();
    const endpoint = `/api/transactions/history${qs ? `?${qs}` : ''}`;
    return apiRequest<PagedResult<TransactionItem>>(endpoint);
  },
};
