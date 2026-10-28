export interface TransferRequest {
  receiverWalletNumber: string;
  amount: number;
  notes?: string;
}

export interface TransferResponse {
  referenceId: string;
  senderWalletNumber: string;
  receiverWalletNumber: string;
  amount: number;
  fee: number;
  netCreditedAmount: number;
  senderBalanceAfter: number;
  status: 'Completed' | 'Pending' | 'Failed';
  createdAt: string;
}
