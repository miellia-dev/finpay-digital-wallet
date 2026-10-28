import { apiRequest } from './api';
import type { DepositRequest, WalletDetails } from '../types/wallet';

export const walletService = {
  async getMyWallet(): Promise<WalletDetails> {
    return apiRequest<WalletDetails>('/api/wallets/my-wallet');
  },

  async deposit(payload: DepositRequest): Promise<WalletDetails> {
    return apiRequest<WalletDetails>('/api/wallets/deposit', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },
};
