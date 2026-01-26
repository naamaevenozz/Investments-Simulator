import React, { useEffect, useState, useCallback } from 'react';
import axios from 'axios';
import { Wallet, TrendingUp, Clock, AlertCircle, LogOut, User } from 'lucide-react';

// Interfaces for Type Safety
interface InvestmentOption {
  name: string;
  amount: number;
  expectedReturn: number;
  durationInSeconds: number;
}

interface ActiveInvestment {
  id: string;
  name: string;
  amount: number;
  expectedReturn: number;
  endTime: string;
}

interface UserData {
  username: string;
  balance: number;
  activeInvestments: ActiveInvestment[];
}

const API_BASE = 'http://localhost:5243/api/investment';

function App() {
  const [username, setUsername] = useState<string | null>(sessionStorage.getItem('invest_user'));
  const [loginInput, setLoginInput] = useState('');
  const [userData, setUserData] = useState<UserData | null>(null);
  const [options, setOptions] = useState<InvestmentOption[]>([]);
  const [error, setError] = useState<string | null>(null);

  // Fetch all user-specific data and general options
  const fetchAllData = useCallback(async () => {
    if (!username) return;

    try {
      const [userRes, optRes] = await Promise.all([
        axios.get(`${API_BASE}/user-data?username=${username}`),
        axios.get(`${API_BASE}/options`)
      ]);

      setUserData(userRes.data);
      setOptions(optRes.data);
    } catch (err) {
      console.error("Connection to backend failed", err);
    }
  }, [username]);

  useEffect(() => {
    if (username) {
      fetchAllData();
      const timer = setInterval(fetchAllData, 1000);
      return () => clearInterval(timer);
    }
  }, [username, fetchAllData]);

  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    if (loginInput.trim()) {
      sessionStorage.setItem('invest_user', loginInput);
      setUsername(loginInput);
    }
  };

  const handleLogout = () => {
    sessionStorage.removeItem('invest_user');
    setUsername(null);
    setUserData(null);
    window.location.reload();
  };

  const handleInvest = async (optionName: string) => {
    try {
      setError(null);
      await axios.post(`${API_BASE}/invest`, {
        username: username,
        optionName: optionName
      });
      fetchAllData();
    } catch (err: any) {
      setError(err.response?.data?.message || "Investment failed");
    }
  };

  // --- LOGIN VIEW ---
  if (!username) {
    return (
        <div style={{ height: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'center', background: '#f0f4f8' }}>
          <form onSubmit={handleLogin} style={{ background: 'white', padding: '40px', borderRadius: '15px', boxShadow: '0 10px 25px rgba(0,0,0,0.1)', width: '100%', maxWidth: '400px' }}>
            <h2 style={{ color: '#005f6b', marginBottom: '20px', textAlign: 'center' }}>Siemens Investment Portal</h2>
            <div style={{ marginBottom: '20px' }}>
              <label style={{ display: 'block', marginBottom: '8px', fontSize: '14px' }}>Enter Username to Start</label>
              <input
                  type="text"
                  value={loginInput}
                  onChange={(e) => setLoginInput(e.target.value)}
                  style={{ width: '100%', padding: '12px', borderRadius: '8px', border: '1px solid #ddd', boxSizing: 'border-box' }}
                  placeholder="e.g. JohnDoe"
                  required
              />
            </div>
            <button type="submit" style={{ width: '100%', background: '#005f6b', color: 'white', border: 'none', padding: '12px', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold' }}>
              Login / Create Account
            </button>
          </form>
        </div>
    );
  }

  // --- DASHBOARD VIEW ---
  return (
      <div style={{ maxWidth: '1000px', margin: '0 auto', padding: '40px', fontFamily: 'Segoe UI, Tahoma, sans-serif' }}>
        <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '40px', borderBottom: '2px solid #eee', paddingBottom: '20px' }}>
          <div>
            <h1 style={{ color: '#005f6b', margin: 0 }}>Siemens Simulator</h1>
            <div style={{ display: 'flex', alignItems: 'center', gap: '5px', color: '#64748b', fontSize: '14px' }}>
              <User size={14} /> Welcome, <strong>{username}</strong>
            </div>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '20px' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', background: '#e6f4f1', padding: '10px 20px', borderRadius: '30px' }}>
              <Wallet color="#005f6b" />
              <span style={{ fontSize: '18px', fontWeight: 'bold' }}>${userData?.balance.toFixed(2) || '0.00'}</span>
            </div>
            <button onClick={handleLogout} style={{ background: 'none', border: 'none', color: '#ef4444', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '5px' }}>
              <LogOut size={18} /> Logout
            </button>
          </div>
        </header>

        {error && (
            <div style={{ background: '#fee2e2', color: '#b91c1c', padding: '15px', borderRadius: '8px', marginBottom: '20px', display: 'flex', alignItems: 'center', gap: '10px' }}>
              <AlertCircle size={20} /> {error}
            </div>
        )}

        <div style={{ display: 'grid', gridTemplateColumns: '2fr 1fr', gap: '30px' }}>

          {/* INVESTMENT TABLE */}
          <section>
            <h3 style={{ display: 'flex', alignItems: 'center', gap: '10px' }}><TrendingUp size={20} /> Market Options</h3>
            <div style={{ background: 'white', borderRadius: '12px', boxShadow: '0 4px 6px rgba(0,0,0,0.05)', overflow: 'hidden', border: '1px solid #e2e8f0' }}>
              <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                <thead style={{ background: '#f8fafc' }}>
                <tr>
                  <th style={{ padding: '15px', textAlign: 'left' }}>Plan</th>
                  <th style={{ padding: '15px', textAlign: 'left' }}>Price</th>
                  <th style={{ padding: '15px', textAlign: 'left' }}>ROI</th>
                  <th style={{ padding: '15px', textAlign: 'center' }}>Action</th>
                </tr>
                </thead>
                <tbody>
                {options.map(opt => (
                    <tr key={opt.name} style={{ borderTop: '1px solid #eee' }}>
                      <td style={{ padding: '15px', fontWeight: 'bold' }}>{opt.name}</td>
                      <td style={{ padding: '15px' }}>${opt.amount}</td>
                      <td style={{ padding: '15px', color: '#16a34a', fontWeight: 'bold' }}>+${(opt.expectedReturn - opt.amount)}</td>
                      <td style={{ padding: '15px', textAlign: 'center' }}>
                        <button
                            onClick={() => handleInvest(opt.name)}
                            style={{ background: '#005f6b', color: 'white', border: 'none', padding: '8px 16px', borderRadius: '6px', cursor: 'pointer' }}>
                          Invest
                        </button>
                      </td>
                    </tr>
                ))}
                </tbody>
              </table>
            </div>
          </section>

          {/* ACTIVE INVESTMENTS PANEL */}
          <section>
            <h3 style={{ display: 'flex', alignItems: 'center', gap: '10px' }}><Clock size={20} /> Active Portfolio</h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px' }}>
              {!userData?.activeInvestments || userData.activeInvestments.length === 0 ? (
                  <div style={{ padding: '30px', textAlign: 'center', color: '#94a3b8', border: '2px dashed #e2e8f0', borderRadius: '12px' }}>
                    No investments running
                  </div>
              ) : (
                  userData.activeInvestments.map(inv => (
                      <div key={inv.id} style={{ background: '#fff', border: '1px solid #e2e8f0', padding: '15px', borderRadius: '12px' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                          <span style={{ fontWeight: 'bold' }}>{inv.name}</span>
                          <span style={{ color: '#16a34a', fontSize: '14px' }}>→ ${inv.expectedReturn}</span>
                        </div>
                        <div style={{ height: '6px', background: '#f1f5f9', borderRadius: '3px', overflow: 'hidden' }}>
                          <div style={{ height: '100%', background: '#005f6b', width: '100%' }}></div>
                        </div>
                        <div style={{ fontSize: '11px', color: '#94a3b8', marginTop: '8px' }}>Processing...</div>
                      </div>
                  ))
              )}
            </div>
          </section>
        </div>
      </div>
  );
}

export default App;