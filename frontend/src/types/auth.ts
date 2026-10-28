export type UserRole = 'Customer' | 'Admin';

export interface UserSession {
  userId: string;
  fullName: string;
  email: string;
  role: UserRole;
  walletNumber: string;
  token: string;
  expiresAt: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phoneNumber: string;
  password: string;
  role?: number; // 1 = Customer, 2 = Admin
}

export interface LoginRequest {
  email: string;
  password: string;
}
