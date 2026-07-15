// src/App.tsx
import { useEffect, useState, useRef } from 'react';
import { type ChatMessagePayload, type DbFollower, type FollowerListPayload } from './types'; 


export default function App() {
  const [messages, setMessages] = useState<ChatMessagePayload[]>([]);
  const [followers, setFollowers] = useState<DbFollower[]>([]);

  const socketRef = useRef<WebSocket | null>(null);
  // 2. Lifecycle Hook (Runs automatically when the web page loads)
  useEffect(() => {
    const socket = new WebSocket('ws://192.168.1.102:8085/');
    socketRef.current = socket;

    socket.onopen = () => {
      console.log('Successfully connected to SlytaBot Backend!');

      const requestPayload = {
        Action: "GetFollowers"
      };
      socket.send(JSON.stringify(requestPayload));
      console.log('Requested follower list from backend database.')
    };

    socket.onmessage = (event) => {
      const rawData = JSON.parse(event.data);

      if (rawData.Type === 'ChatMessage') {

        setMessages((prev) => [...prev, rawData as ChatMessagePayload]);
      }
      else if (rawData.Type === 'FollowerList') {
        const listPayload = rawData as FollowerListPayload;
        setFollowers(listPayload.Data);
      }
    };

    // Clean up the connection if the browser tab closes
    return () => socket.close();
  }, []);

const handleOpenArena = () => {
  if (socketRef.current && socketRef.current.readyState === WebSocket.OPEN)
  {
    const actionPayload =
    {
      Action: 'OpenArena',
      TargetChannel: 'remnapi'
    };

    socketRef.current.send(JSON.stringify(actionPayload));
    console.log('Sent OpenArena action to C# backend')
  } else
  {
    alert('WebSocket is not connected!');
  }
};

return (
    <div style={{ padding: '20px', backgroundColor: '#1e1e1e', color: '#fff', minHeight: '100vh' }}>
      <h1>SlytaDX Control Center</h1>
      
      {/* 4. The Action Button */}
      <button 
        onClick={handleOpenArena}
        style={{ padding: '10px 20px', fontSize: '16px', backgroundColor: '#9146FF', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', marginBottom: '20px' }}
      >
        📢 Open Smash Arena (Broadcast to Discord)
      </button>
      
      <div style={{ flex: '1', minWidth: '300px' }}>
          <h2>Live Chat</h2>
          <div style={{ border: '1px solid #444', borderRadius: '8px', padding: '15px', height: '450px', overflowY: 'auto', backgroundColor: '#121212' }}>
            {messages.map((msg) => (
              <div key={msg.MessageID} style={{ marginBottom: '10px', fontSize: '16px' }}>
                <strong style={{ color: msg.UserColor }}>{msg.Username}: </strong>
                <span>{msg.Message}</span>
              </div>
            ))}
          </div>
        </div>
        
<div style={{ flex: '1.5', minWidth: '400px' }}>
          <h2>Top Active Database Viewers</h2>
          <div style={{ border: '1px solid #444', borderRadius: '8px', padding: '15px', height: '450px', overflowY: 'auto', backgroundColor: '#121212' }}>
            <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left' }}>
              <thead>
                <tr style={{ borderBottom: '2px solid #444', color: '#888' }}>
                  <th style={{ padding: '8px' }}>Username</th>
                  <th style={{ padding: '8px' }}>Mod</th>
                  <th style={{ padding: '8px' }}>Followed At</th>
                  <th style={{ padding: '8px', textAlign: 'right' }}>Total Messages</th>
                </tr>
              </thead>
              <tbody>
                {followers.map((follower) => (
                  <tr key={follower.UserID} style={{ borderBottom: '1px solid #2a2a2a' }}>
                    <td style={{ padding: '8px', fontWeight: 'bold' }}>
                      {follower.Username}
                    </td>
                    <td style={{ padding: '8px' }}>
                      {follower.IsModerator === 1 ? (
                        <span style={{ backgroundColor: '#04d361', color: '#000', padding: '2px 6px', borderRadius: '4px', fontSize: '12px', fontWeight: 'bold' }}>
                          MOD
                        </span>
                      ) : (
                        <span style={{ color: '#555', fontSize: '12px' }}>no</span>
                      )}
                    </td>
                    <td style={{ padding: '8px', fontSize: '14px', color: '#aaa' }}>
                      {new Date(follower.FollowDate).toLocaleDateString()}
                    </td>
                    <td style={{ padding: '8px', textAlign: 'right', fontWeight: 'bold', color: '#9146FF' }}>
                      {follower.Message_Count}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

      </div>
  );
}
