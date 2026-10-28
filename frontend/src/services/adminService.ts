import { apiRequest } from './api';
import type { FreezeWalletRequest, WalletDetails } from '../types/wallet';

export const adminService = {
  async freezeWallet(walletId: string, payload: FreezeWalletRequest): Promise<WalletDetails> {
    return apiRequest<WalletDetails>(`/api/admin/wallets/${walletId}/freeze`, {
      method: 'PATCH',
      body: JSON.stringify(payload),
    });
  },
};
