import { apiRequest } from './api';
import type { LoginRequest, RegisterRequest, UserSession } from '../types/auth';

export const authService = {
  async login(credentials: LoginRequest): Promise<UserSession> {
    const session = await apiRequest<UserSession>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(credentials),
    });
    localStorage.setItem('finpay_token', session.token);
    localStorage.setItem('finpay_user', JSON.stringify(session));
    return session;
  },

  async register(payload: RegisterRequest): Promise<UserSession> {
    const session = await apiRequest<UserSession>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
    localStorage.setItem('finpay_token', session.token);
    localStorage.setItem('finpay_user', JSON.stringify(session));
    return session;
  },

  logout(): void {
    localStorage.removeItem('finpay_token');
    localStorage.removeItem('finpay_user');
  },

  getStoredSession(): UserSession | null {
    const raw = localStorage.getItem('finpay_user');
    if (!raw) return null;
    try {
      return JSON.parse(raw) as UserSession;
    } catch {
      return null;
    }
  },
};
