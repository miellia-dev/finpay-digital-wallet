import React from 'react';
import { useAuth } from '../../context/AuthContext';
import { ShieldCheck, User as UserIcon } from 'lucide-react';

export const Header: React.FC = () => {
  const { user } = useAuth();

  return (
    <header className="h-16 bg-white border-b border-slate-200/80 px-8 flex items-center justify-between shrink-0">
      <div>
        <h2 className="text-sm font-semibold text-slate-800">
          Digital Banking Portal
        </h2>
        <p className="text-xs text-slate-400">
          Secure Core Engine Connected
        </p>
      </div>

      <div className="flex items-center space-x-4">
        {/* Role Pill */}
        <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
          {user?.role === 'Admin' ? (
            <>
              <ShieldCheck className="w-3.5 h-3.5 text-rose-600 mr-1.5" />
              <span className="text-rose-700">Administrator</span>
            </>
          ) : (
            <>
              <UserIcon className="w-3.5 h-3.5 text-emerald-600 mr-1.5" />
              <span>Standard Customer</span>
            </>
          )}
        </span>

        {/* User Identity Pill */}
        <div className="text-right">
          <p className="text-xs font-semibold text-slate-800">{user?.fullName}</p>
          <p className="text-[11px] text-slate-400">{user?.email}</p>
        </div>
      </div>
    </header>
  );
};
