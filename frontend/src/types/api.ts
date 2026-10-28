export interface ApiResponse<T = any> {
  success: boolean;
  message: string;
  data: T;
  errorCode?: string | null;
  timestamp: string;
}

export interface PagedResult<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}
