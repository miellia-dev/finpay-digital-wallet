import { apiRequest } from './api';
import type { TransferRequest, TransferResponse } from '../types/transfer';

export const transferService = {
  async transferP2P(payload: TransferRequest): Promise<TransferResponse> {
    return apiRequest<TransferResponse>('/api/transfers/p2p', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },
};
