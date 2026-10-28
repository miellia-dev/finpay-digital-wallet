export type WalletType = 'Basic' | 'Merchant' | 1 | 2;

export interface WalletDetails {
  walletId: string;
  userId: string;
  walletNumber: string;
  balance: number;
  walletType: WalletType;
  isFrozen: boolean;
  dailyTransferLimit: number;
  transferredToday: number;
  remainingDailyLimit: number;
  createdAt: string;
}

export interface DepositRequest {
  amount: number;
  notes?: string;
}

export interface FreezeWalletRequest {
  isFrozen: boolean;
  reason?: string;
}
