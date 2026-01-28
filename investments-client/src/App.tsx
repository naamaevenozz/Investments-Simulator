import React, { useEffect, useState, useCallback, useRef } from 'react';
import axios from 'axios';
import { Wallet, Clock, AlertCircle, RefreshCcw, Wifi, WifiOff } from 'lucide-react';
import * as signalR from '@microsoft/signalr'; 

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
const SIGNALR_HUB = 'http://localhost:5243/investmentHub';

function App() {
  const [username, setUsername] = useState<string | null>(null);
  const [loginInput, setLoginInput] = useState('');
  const [userData, setUserData] = useState<UserData | null>(null);
  const [options, setOptions] = useState<InvestmentOption[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null); 
  const [currentTime, setCurrentTime] = useState(Date.now());
  const [lastUpdate, setLastUpdate] = useState<string>('--:--:--');

  const [isConnected, setIsConnected] = useState(false);
  const connectionRef = useRef<signalR.HubConnection | null>(null); // ref for SignalR connection

  useEffect(() => {
    const stored = sessionStorage.getItem('invest_user');
    if (stored) setUsername(stored);
  }, []); // The [] ensures this runs only once on mount

  useEffect(() => {
    const timer = setInterval(() => {
      setCurrentTime(Date.now());
    }, 1000);
    return () => clearInterval(timer);
  }, []);

  useEffect(() => {
    if (!username) return;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(SIGNALR_HUB, {
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect() // Try to reconnect automatically
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.onreconnecting(() => {
      console.log('Reconnecting to SignalR...');
      setIsConnected(false);
    });

    connection.onreconnected(() => {
      console.log('Reconnected to SignalR');
      setIsConnected(true);
      connection.invoke('SubscribeToUpdates', username);
    });

    connection.onclose(() => {
      console.log('SignalR connection closed');
      setIsConnected(false);
    });

    connection.on('SubscriptionConfirmed', (data: any) => {
      console.log('Subscribed to updates:', data);
      setSuccess('Connected to real-time updates!');
      setTimeout(() => setSuccess(null), 3000);
    });

    connection.on('InvestmentStarted', (data: { optionName: any; newBalance: any; activeInvestments: any; }) => {
      console.log('Investment started:', data);
      setSuccess(`Investment in ${data.optionName} started!`);
      setTimeout(() => setSuccess(null), 3000);

      setUserData(prev => prev ? {
        ...prev,
        balance: data.newBalance,
        activeInvestments: data.activeInvestments
      } : null);

      updateLastUpdateTime();
    });

    connection.on('InvestmentCompleted', (data: { message: any; payout: any; newBalance: any; activeInvestments: any; }) => {
      console.log('Investment completed:', data);
      setSuccess(`${data.message} +$${data.payout}`);
      setTimeout(() => setSuccess(null), 5000);

      setUserData(prev => prev ? {
        ...prev,
        balance: data.newBalance,
        activeInvestments: data.activeInvestments
      } : null);

      updateLastUpdateTime();
    });

    connection.on('InvestmentFailed', (data: { message: React.SetStateAction<string | null>; }) => {
      console.log('Investment failed:', data);
      setError(data.message);
      setTimeout(() => setError(null), 5000);
    });

    connection.start()
        .then(() => {
          console.log('SignalR connected');
          setIsConnected(true);
          connectionRef.current = connection;

          return connection.invoke('SubscribeToUpdates', username);
        })
        .catch((err: any) => {
          console.error('SignalR connection failed:', err);
          setError('Real-time connection failed. Using polling mode.');
          setTimeout(() => setError(null), 5000);
        });

    return () => {
      // Clean up the connection on unmount
      if (connection) {
        connection.stop();
      }
    };
  }, [username]);

  const updateLastUpdateTime = () => {
    const now = new Date();
    setLastUpdate(`${now.toLocaleDateString('en-GB')}, ${now.toLocaleTimeString('en-GB')}`);
  };
  
  // use callback to memoize fetchUserData function
  const fetchAllData = useCallback(async () => {
    if (!username) return;

    try {
      const [userRes, optRes] = await Promise.all([
        axios.get(`${API_BASE}/user-data?username=${username}`),
        axios.get(`${API_BASE}/options`)
      ]);

      setUserData(userRes.data);
      setOptions(optRes.data);
      updateLastUpdateTime();
    } catch (err) {
      console.error("Connection to backend failed", err);
    }
  }, [username]);

  useEffect(() => {
    if (username) {
      fetchAllData();
      const interval = setInterval(fetchAllData, 10000); 
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
        return "Completing...";
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
    if (trimmed.length < 3 || !/^[a-zA-Z]+$/.test(trimmed)) {
      setError("Username must be at least 3 English letters");
      return;
    }
    setError(null);
    sessionStorage.setItem('invest_user', trimmed);
    setUsername(trimmed);
  };

  const handleLogout = () => {
    if (connectionRef.current) {
      connectionRef.current.stop();
    }
    sessionStorage.removeItem('invest_user');
    setUsername(null);
    setUserData(null);
    setIsConnected(false);
  };

  const handleInvest = async (optionName: string) => {
    try {
      setError(null);
      setSuccess(null);

      const response = await axios.post(`${API_BASE}/invest`, {
        username: username,
        optionName: optionName
      });

      if (response.status === 202) {
        setSuccess('Investment request submitted! Processing...');
      }
    } catch (err: any) {
      setError(err.response?.data?.message || "Investment failed");
      setTimeout(() => setError(null), 5000);
    }
  };

  if (!username) {
    return (
        <div style={{ height: '100vh', display: 'flex', justifyContent: 'center', alignItems: 'center', background: '#f0f4f8' }}>
          <form onSubmit={handleLogin} style={{ background: 'white', padding: '40px', borderRadius: '15px', boxShadow: '0 10px 25px rgba(0,0,0,0.1)', width: '100%', maxWidth: '400px' }}>
            <h2 style={{ color: '#005f6b', marginBottom: '20px', textAlign: 'center' }}>Investment Simulator</h2>
            <input
                type="text"
                value={loginInput}
                onChange={(e) => setLoginInput(e.target.value)}
                style={{
                  width: '100%',
                  padding: '12px',
                  borderRadius: '8px',
                  border: '1px solid #ddd',
                  marginBottom: '10px',
                  boxSizing: 'border-box'
                }}
                placeholder="Enter Username"
                required
            />
            {error && <div style={{ color: 'red', fontSize: '12px', marginBottom: '10px' }}>{error}</div>}
            <button
                type="submit"
                style={{
                  width: '100%',
                  background: '#005f6b',
                  color: 'white',
                  border: 'none',
                  padding: '12px',
                  borderRadius: '8px',
                  cursor: 'pointer',
                  fontWeight: 'bold',
                  boxSizing: 'border-box'
                }}
            >
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
            <div style={{ display: 'flex', alignItems: 'center', gap: '10px', marginTop: '8px' }}>
              {/* NEW: Connection status indicator */}
              <div style={{ display: 'flex', alignItems: 'center', gap: '5px', fontSize: '13px', color: isConnected ? '#10b981' : '#ef4444' }}>
                {isConnected ? <Wifi size={14} /> : <WifiOff size={14} />}
                <span>{isConnected ? 'Real-time updates active' : 'Connecting...'}</span>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: '5px', color: '#64748b', fontSize: '13px' }}>
                <RefreshCcw size={14} />
                <span>Last Update: {lastUpdate}</span>
              </div>
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

        {/* NEW: Success messages */}
        {success && (
            <div style={{ background: '#d1fae5', color: '#065f46', padding: '15px', borderRadius: '8px', marginBottom: '20px', display: 'flex', alignItems: 'center', gap: '10px', border: '1px solid #10b981' }}>
              <span style={{ fontSize: '20px' }}>✓</span> {success}
            </div>
        )}

        {error && (
            <div style={{ background: '#fee2e2', color: '#b91c1c', padding: '15px', borderRadius: '8px', marginBottom: '20px', display: 'flex', alignItems: 'center', gap: '10px' }}>
              <AlertCircle size={20} /> {error}
            </div>
        )}

        <div style={{ display: 'grid', gridTemplateColumns: '1.5fr 1fr', gap: '30px' }}>

          {/* Section: Available Investments */}
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
                          color: 'white',
                          border: 'none',
                          padding: '10px 20px',
                          borderRadius: '8px',
                          cursor: (userData && userData.balance < opt.amount) ? 'not-allowed' : 'pointer',
                          fontWeight: 'bold'
                        }}
                    >
                      Invest
                    </button>
                  </div>
              ))}
            </div>
          </section>

          {/* Section: Current Investments */}
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