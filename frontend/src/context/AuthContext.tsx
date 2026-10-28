import React, { createContext, useContext, useState, useEffect } from 'react';
import type { UserSession, LoginRequest, RegisterRequest } from '../types/auth';
import { authService } from '../services/authService';

interface AuthContextType {
  user: UserSession | null;
  isAuthenticated: boolean;
  isAdmin: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<UserSession>;
  register: (payload: RegisterRequest) => Promise<UserSession>;
  logout: () => void;
  updateUserSession: (updated: Partial<UserSession>) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserSession | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const session = authService.getStoredSession();
    if (session) {
      setUser(session);
    }
    setIsLoading(false);
  }, []);

  const login = async (credentials: LoginRequest) => {
    const session = await authService.login(credentials);
    setUser(session);
    return session;
  };

  const register = async (payload: RegisterRequest) => {
    const session = await authService.register(payload);
    setUser(session);
    return session;
  };

  const logout = () => {
    authService.logout();
    setUser(null);
  };

  const updateUserSession = (updated: Partial<UserSession>) => {
    if (!user) return;
    const newSession = { ...user, ...updated };
    setUser(newSession);
    localStorage.setItem('finpay_user', JSON.stringify(newSession));
  };

  const isAuthenticated = !!user?.token;
  const isAdmin = user?.role === 'Admin';

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated,
        isAdmin,
        isLoading,
        login,
        register,
        logout,
        updateUserSession,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
