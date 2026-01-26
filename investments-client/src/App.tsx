import React, { useEffect, useState, useCallback } from 'react';
import axios from 'axios';
import { Wallet, Clock, AlertCircle, RefreshCcw } from 'lucide-react';

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
  endTime: string; // ISO String from Backend
}

interface UserData {
  username: string;
  balance: number;
  activeInvestments: ActiveInvestment[];
}

const API_BASE = 'http://localhost:5243/api/investment';

function App() {
  const [username, setUsername] = useState<string | null>(null);
  const [loginInput, setLoginInput] = useState('');
  const [userData, setUserData] = useState<UserData | null>(null);
  const [options, setOptions] = useState<InvestmentOption[]>([]);
  const [error, setError] = useState<string | null>(null);

  // State for the ticking clock (triggers re-render every second for "Ends in") [cite: 35]
  const [currentTime, setCurrentTime] = useState(Date.now());

  // State for the "Last Update" timestamp 
  const [lastUpdate, setLastUpdate] = useState<string>('--:--:--');

  useEffect(() => {
    const stored = localStorage.getItem('invest_user');
    if (stored) setUsername(stored);
  }, []);

  // Timer effect: Updates every 1 second to ensure "Ends in" countdown moves [cite: 35]
  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(Date.now());
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  const fetchAllData = useCallback(async () => {
    if (!username) return;

    try {
      const [userRes, optRes] = await Promise.all([
        axios.get(`${API_BASE}/user-data?username=${username}`),
        axios.get(`${API_BASE}/options`)
      ]);

      setUserData(userRes.data);
      setOptions(optRes.data);

      // Requirement: Update "Last Update" display 
      const now = new Date();
      setLastUpdate(`${now.toLocaleDateString('en-GB')}, ${now.toLocaleTimeString('en-GB')}`);

    } catch (err) {
      console.error("Connection to backend failed", err);
    }
  }, [username]);

  useEffect(() => {
    if (username) {
      fetchAllData();
      const interval = setInterval(fetchAllData, 2000); // Poll BE every 2s [cite: 56]
      return () => clearInterval(interval);
    }
  }, [username, fetchAllData]);

  const getTimeRemaining = (endTimeStr: string): string => {
    try {
      const utcStr = endTimeStr.endsWith('Z') ? endTimeStr : endTimeStr + 'Z';
      const end = new Date(utcStr).getTime();
      const now = Date.now();
      const diff = end - now;

      if (isNaN(end)) {
        console.error('Invalid date:', endTimeStr);
        return "Invalid time";
      }

      if (diff <= 0) {
        return "Finishing...";
      }

      const seconds = Math.floor(diff / 1000);
      const minutes = Math.floor(seconds / 60);
      const remainingSeconds = seconds % 60;

      if (minutes > 0) {
        return `${minutes}m ${remainingSeconds}s`;
      }
      return `${seconds}s`;
    } catch (error) {
      console.error('Error calculating time:', error);
      return "Error";
    }
  };
  const handleLogin = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = loginInput.trim();
    // Validation: 3 chars min, English letters only [cite: 28]
    if (trimmed.length < 3 || !/^[a-zA-Z]+$/.test(trimmed)) {
      setError("Username must be at least 3 English letters");
      return;
    }
    setError(null);
    localStorage.setItem('invest_user', trimmed);
    setUsername(trimmed);
  };

  const handleLogout = () => {
    localStorage.removeItem('invest_user');
    setUsername(null);
    setUserData(null);
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
      // Show clear response from BE [cite: 71]
      setError(err.response?.data?.message || "Investment failed");
    }
  };

  if (!username) {
    return (
        <div style={{ height: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'center', background: '#f0f4f8' }}>
          <form onSubmit={handleLogin} style={{ background: 'white', padding: '40px', borderRadius: '15px', boxShadow: '0 10px 25px rgba(0,0,0,0.1)', width: '100%', maxWidth: '400px' }}>
            <h2 style={{ color: '#005f6b', marginBottom: '20px', textAlign: 'center' }}>Investment Portal</h2>
            <input
                type="text"
                value={loginInput}
                onChange={(e) => setLoginInput(e.target.value)}
                style={{ width: '100%', padding: '12px', borderRadius: '8px', border: '1px solid #ddd', marginBottom: '10px' ,
                  boxSizing: 'border-box'}}
                placeholder="Enter Username"
                required
            />
            {error && <div style={{ color: 'red', fontSize: '12px', marginBottom: '10px' }}>{error}</div>}
            <button type="submit" style={{ width: '100%', background: '#005f6b', color: 'white', border: 'none', padding: '12px', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold',
              boxSizing: 'border-box' }}>
              Login
            </button>
          </form>
        </div>
    );
  }

  return (
      <div style={{ maxWidth: '1100px', margin: '0 auto', padding: '40px', fontFamily: 'Segoe UI, sans-serif' }}>
        <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '40px', borderBottom: '2px solid #eee', paddingBottom: '20px' }}>
          <div>
            <h1 style={{ color: '#005f6b', margin: 0 }}>Hello, {username}</h1>
            <div style={{ display: 'flex', alignItems: 'center', gap: '5px', color: '#64748b', marginTop: '5px', fontSize: '14px' }}>
              <RefreshCcw size={14} />
              <span>Last Update: {lastUpdate}</span>
            </div>
          </div>

          <div style={{ textAlign: 'right' }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', background: '#e6f4f1', padding: '15px 25px', borderRadius: '12px', border: '1px solid #005f6b' }}>
              <Wallet color="#005f6b" size={24} />
              <div>
                <div style={{ fontSize: '12px', color: '#005f6b', fontWeight: 'bold' }}>CURRENT BALANCE</div>
                <div style={{ fontSize: '24px', fontWeight: 'bold' }}>${userData?.balance.toLocaleString() || '0'}</div>
              </div>
            </div>
            <button onClick={handleLogout} style={{ marginTop: '10px', background: 'none', border: 'none', color: '#ef4444', cursor: 'pointer', fontSize: '14px' }}>
              Logout
            </button>
          </div>
        </header>

        {error && (
            <div style={{ background: '#fee2e2', color: '#b91c1c', padding: '15px', borderRadius: '8px', marginBottom: '20px', display: 'flex', alignItems: 'center', gap: '10px' }}>
              <AlertCircle size={20} /> {error}
            </div>
        )}

        <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: '30px' }}>

          {/* Section: Available Investments [cite: 38, 103] */}
          <section>
            <h3 style={{ borderBottom: '2px solid #005f6b', paddingBottom: '10px' }}>Available Investments</h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px', marginTop: '20px' }}>
              {options.map(opt => (
                  <div key={opt.name} style={{ background: 'white', padding: '20px', borderRadius: '12px', boxShadow: '0 2px 4px rgba(0,0,0,0.05)', display: 'flex', justifyContent: 'space-between', alignItems: 'center', border: '1px solid #e2e8f0' }}>
                    <div>
                      <div style={{ fontWeight: 'bold', fontSize: '18px' }}>{opt.name}</div>
                      <div style={{ fontSize: '14px', color: '#64748b' }}>Cost: ${opt.amount} | Return: ${opt.expectedReturn}</div>
                      <div style={{ fontSize: '14px', color: '#64748b' }}>Duration: {opt.durationInSeconds}s</div>
                    </div>
                    <button
                        onClick={() => handleInvest(opt.name)}
                        disabled={!!(userData && userData.balance < opt.amount)}
                        style={{
                          background: (userData && userData.balance < opt.amount) ? '#cbd5e1' : '#005f6b',
                          color: 'white', border: 'none', padding: '10px 20px', borderRadius: '8px', cursor: 'pointer', fontWeight: 'bold'
                        }}
                    >
                      Invest
                    </button>
                  </div>
              ))}
            </div>
          </section>

          {/* Section: Current Investments [cite: 37, 99] */}
          <section>
            <h3 style={{ borderBottom: '2px solid #005f6b', paddingBottom: '10px' }}>Current Investments</h3>
            <div style={{ display: 'flex', flexDirection: 'column', gap: '15px', marginTop: '20px' }}>
              {!userData?.activeInvestments?.length ? (
                  <div style={{ textAlign: 'center', padding: '40px', color: '#94a3b8', border: '2px dashed #e2e8f0', borderRadius: '12px' }}>
                    No active investments
                  </div>
              ) : (
                  userData.activeInvestments.map(inv => (
                      <div key={inv.id} style={{ background: '#f8fafc', padding: '15px', borderRadius: '12px', borderLeft: '5px solid #005f6b' }}>
                        <div style={{ display: 'flex', justifyContent: 'space-between', fontWeight: 'bold', marginBottom: '8px' }}>
                          <span>{inv.name}</span>
                          <span style={{ color: '#16a34a' }}>+${inv.expectedReturn}</span>
                        </div>

                        <div style={{ fontSize: '13px', color: '#64748b', marginBottom: '4px' }}>
                          <span style={{ fontWeight: 'bold' }}>ID:</span> {inv.id.substring(0, 8)}
                        </div>

                        <div style={{ fontSize: '13px', color: '#64748b', marginBottom: '4px' }}>
                          <span style={{ fontWeight: 'bold' }}>Amount Invested:</span> ${inv.amount}
                        </div>

                        <div style={{ fontSize: '13px', color: '#64748b', marginTop: '5px' }}>
                          <Clock size={12} style={{ display: 'inline', marginRight: '4px' }} />
                          Ends in: <span style={{ fontWeight: 'bold', color: '#005f6b' }}>{getTimeRemaining(inv.endTime)}</span>
                        </div>
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