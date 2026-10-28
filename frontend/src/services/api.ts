import type { ApiResponse } from '../types/api';

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5088';

export class ApiError extends Error {
  public statusCode: number;
  public errorCode?: string | null;

  constructor(message: string, statusCode: number, errorCode?: string | null) {
    super(message);
    this.name = 'ApiError';
    this.statusCode = statusCode;
    this.errorCode = errorCode;
  }
}

export async function apiRequest<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const url = `${BASE_URL}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;

  const token = localStorage.getItem('finpay_token');

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    ...(options.headers as Record<string, string>),
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  let response: Response;
  try {
    response = await fetch(url, {
      ...options,
      headers,
    });
  } catch (networkErr: any) {
    throw new ApiError(
      `Unable to connect to FinPay Core API at ${BASE_URL}. Ensure the backend service is running.`,
      0,
      'NETWORK_DISCONNECTED'
    );
  }

  let data: ApiResponse<T> | null = null;
  const text = await response.text();
  if (text) {
    try {
      data = JSON.parse(text) as ApiResponse<T>;
    } catch {
      // Non-JSON response
    }
  }

  if (!response.ok) {
    const errorMsg = data?.message || response.statusText || 'Request failed';
    const errorCode = data?.errorCode || null;
    throw new ApiError(errorMsg, response.status, errorCode);
  }

  if (data && typeof data === 'object' && 'data' in data) {
    return data.data;
  }

  return (data as unknown) as T;
}
