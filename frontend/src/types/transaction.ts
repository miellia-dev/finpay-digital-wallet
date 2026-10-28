export interface TransactionItem {
  id: string;
  referenceId: string;
  senderWalletNumber?: string | null;
  receiverWalletNumber: string;
  amount: number;
  fee: number;
  type: string;
  status: 'Completed' | 'Pending' | 'Failed';
  createdAt: string;
  direction: 'Debit' | 'Credit' | 'Self';
  balanceAfter?: number | null;
}

export interface TransactionHistoryQuery {
  pageNumber?: number;
  pageSize?: number;
  startDate?: string;
  endDate?: string;
  type?: number;
}
