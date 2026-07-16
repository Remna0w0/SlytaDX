// src/App.tsx
import { useEffect, useState, useRef, type JSX } from 'react';
import { type ChatMessagePayload, type DbFollower, type FollowerListPayload, type StreamLiveStatusPayload } from './types'; 


export default function App() {
  const [messages, setMessages] = useState<ChatMessagePayload[]>([]);
  const [followers, setFollowers] = useState<DbFollower[]>([]);
  const [arenaCode, setArenaCode] = useState<string>('');
  const [liveStatus, setLiveStatus] = useState<boolean>(false);



  const socketRef = useRef<WebSocket | null>(null);
  const chatContainerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (chatContainerRef.current) {
    chatContainerRef.current.scrollTop = chatContainerRef.current.scrollHeight;
  }
}, [messages]);


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
      else if (rawData.Type === 'StreamLiveStatus') {
        const isLive = rawData as StreamLiveStatusPayload;
        setLiveStatus(isLive.LiveStatus);
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

const handleUpdateArenaCode = () => {
  if (!arenaCode.trim()) {
    alert('Please enter a valid Arena Code!');
    return;
  }

  if (socketRef.current && socketRef.current.readyState === WebSocket.OPEN) {
    const actionPayload = {
      Action: 'UpdateArenaCode',
      Code: arenaCode
    };

    socketRef.current.send(JSON.stringify(actionPayload));
    console.log (`Sent request to update Arena Code to: ${arenaCode}`);
    setArenaCode('');
  } else {
    alert('Websocket is Offline!');
  }
};

function convertMessageEmotes(message : string) : (string | JSX.Element)[] {
  let words = message.split(' ')
   return words.map((word, index) => {
  if (word.startsWith('{EMOTE:') && word.endsWith('}')) {
    const emoteId = word.replace("{EMOTE:", "").replace("}", "")

    return (
      <img 
      key={index}
      src={`https://static-cdn.jtvnw.net/emoticons/v2/${emoteId}/default/dark/1.0`}
      alt="emote"
      style={{
        width: '24px',
        height: '24px',
        verticalAlign: 'middle',
        margin: '0 2px'
      }}
      />
    );  
  } else {
    return word + ' ';
  }
  
});
}

return (
  <div style={{ padding: '20px', backgroundColor: '#1e1e1e', color: '#fff', minHeight: '100vh', fontFamily: 'sans-serif' }}>
    
    {/* Header & Live Status Indicator Panel */}
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '20px' }}>
      <h1 style={{ margin: 0 }}>SlytaDX Control Center</h1>
      
      {/* 🟢/🔴 Live Status Box */}
      <div style={{ 
        display: 'flex', 
        alignItems: 'center', 
        gap: '8px', 
        backgroundColor: '#252526', 
        padding: '6px 12px', 
        borderRadius: '20px', 
        border: '1px solid #333'
      }}>
        {/* The Indicator Dot */}
        <span style={{
          width: '10px',
          height: '10px',
          borderRadius: '50%',
          display: 'inline-block',
          backgroundColor: liveStatus ? '#04d361' : '#555555', // Bright Green vs. Dark Gray[cite: 12]
          boxShadow: liveStatus ? '0 0 8px #04d361' : 'none' // Gives the green dot a nice "glowing" effect!
        }} />
        
        {/* The Text Status */}
        <span style={{ 
          fontSize: '14px', 
          fontWeight: 'bold', 
          letterSpacing: '0.5px',
          color: liveStatus ? '#04d361' : '#aaaaaa'
        }}>
          {liveStatus ? 'LIVE' : 'OFFLINE'}
        </span>
      </div>
      </div>

      
{/* Utility Control Panel */}
<div style={{ 
  display: 'flex', 
  gap: '15px', 
  alignItems: 'center', 
  backgroundColor: '#252526', 
  padding: '15px', 
  borderRadius: '8px', 
  marginBottom: '20px',
  border: '1px solid #333'
}}>
  
  {/* Arena Code Input & Submit */}
  <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
    <label style={{ fontWeight: 'bold', fontSize: '14px', color: '#ccc' }}>New Arena Code:</label>
    <input 
      type="text" 
      value={arenaCode} 
      onChange={(e) => setArenaCode(e.target.value)} 
      placeholder="e.g., 5Y8X9"
      style={{ 
        padding: '8px 12px', 
        fontSize: '14px', 
        borderRadius: '4px', 
        border: '1px solid #555', 
        backgroundColor: '#1e1e1e', 
        color: '#fff',
        width: '150px'
      }} 
    />
    <button 
      onClick={handleUpdateArenaCode}
      style={{ 
        padding: '8px 16px', 
        backgroundColor: '#04d361', 
        color: '#000', 
        border: 'none', 
        borderRadius: '4px', 
        fontWeight: 'bold', 
        cursor: 'pointer' 
      }}
    >
      💾 Set Arena Code
    </button>
  </div>

  {/* Vertical Divider */}
  <div style={{ width: '1px', height: '30px', backgroundColor: '#444' }}></div>

  {/* Existing Discord Broadcast Button */}
  <button 
    onClick={handleOpenArena}
    style={{ 
      padding: '8px 16px', 
      fontSize: '14px', 
      backgroundColor: '#9146FF', 
      color: 'white', 
      border: 'none', 
      borderRadius: '4px', 
      fontWeight: 'bold',
      cursor: 'pointer' 
    }}
  >
    📢 Broadcast Arena to Discord
  </button>
</div>
      
      <div style={{ display: 'flex', gap: '20px', marginTop: '20px' }}>
      <div style={{ flex: '1', minWidth: '300px' }}>
          <h2>Live Chat</h2>
          <div 
          ref={chatContainerRef} 
          style={{ 
            border: '1px solid #444', 
            borderRadius: '8px', 
            padding: '15px', 
            height: '450px', 
            overflowY: 'auto', 
            backgroundColor: '#121212' 
            }}>
            {messages.map((msg) => (
              <div key={msg.MessageID} style={{ marginBottom: '10px', fontSize: '16px' }}>
                <strong style={{ color: msg.UserColor }}>{msg.Username}: </strong>
                <span style={{ 
                  wordBreak: 'break-word',    
                  overflowWrap: 'anywhere'    
                 }}>
                  {convertMessageEmotes(msg.Message)}</span>
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

      </div>
  );
}
