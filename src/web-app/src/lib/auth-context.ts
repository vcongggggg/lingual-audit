'use client';

import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { UserDto, logoutMezon } from './api';

export interface User {
  id: string;
  mezonUserId: string;
  displayName: string;
  avatarUrl?: string | null;
  role: string;
  status?: string;
  preferredLocale?: string;
  timezone?: string;
  lastSeenAt?: string | null;
  profile?: {
    level?: number;
    streakDays?: number;
    experiencePoints?: number;
    gems?: number;
  } | null;
}

export interface AuthContextType {
  user: User | null;
  token: string | null;
  loading: boolean;
  isAuthenticated: boolean;
  login: (authData: { accessToken: string; user: UserDto }) => void;
  updateUser: (userData: Partial<User>) => void;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType>({
  user: null,
  token: null,
  loading: true,
  isAuthenticated: false,
  login: () => {},
  updateUser: () => {},
  logout: async () => {},
});

export const AuthProvider = ({ children }: { children: ReactNode }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Restore session from localStorage on client-side mount
    try {
      const storedToken = localStorage.getItem('accessToken');
      const storedUser = localStorage.getItem('user');

      if (storedToken && storedUser) {
        setToken(storedToken);
        setUser(JSON.parse(storedUser));
      }
    } catch (e) {
      console.error('Lỗi khi khôi phục phiên đăng nhập:', e);
      localStorage.removeItem('accessToken');
      localStorage.removeItem('user');
    } finally {
      setLoading(false);
    }
  }, []);

  const login = (authData: { accessToken: string; user: UserDto }) => {
    const userToSave: User = {
      id: String(authData.user.id),
      mezonUserId: authData.user.mezonUserId || '',
      displayName: authData.user.displayName || 'Mezon User',
      avatarUrl: authData.user.avatarUrl || null,
      role: authData.user.role || 'learner',
      status: authData.user.status || 'active',
      preferredLocale: authData.user.preferredLocale || 'vi-VN',
      timezone: authData.user.timezone || 'Asia/Ho_Chi_Minh',
      lastSeenAt: authData.user.lastSeenAt || null,
      profile: authData.user.profile || null,
    };

    localStorage.setItem('accessToken', authData.accessToken);
    localStorage.setItem('user', JSON.stringify(userToSave));

    setToken(authData.accessToken);
    setUser(userToSave);
  };

  const updateUser = (userData: Partial<User>) => {
    setUser((prev) => {
      if (!prev) return null;
      const updated = { ...prev, ...userData };
      localStorage.setItem('user', JSON.stringify(updated));
      return updated;
    });
  };

  const logout = async () => {
    const currentToken = token || localStorage.getItem('accessToken');
    if (currentToken) {
      try {
        await logoutMezon(currentToken);
      } catch (err) {
        console.warn('Lỗi thu hồi token từ server (vẫn tiếp tục đăng xuất client):', err);
      }
    }

    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');

    setToken(null);
    setUser(null);
  };

  return React.createElement(
    AuthContext.Provider,
    {
      value: {
        user,
        token,
        loading,
        isAuthenticated: !!user,
        login,
        updateUser,
        logout,
      },
    },
    children
  );
};

export const useAuth = () => useContext(AuthContext);