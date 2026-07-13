// src/App.tsx
import { useEffect, useState, useRef } from 'react';
import { type ChatMessagePayload } from './types'; 


export default function App() {
  const [messages, setMessages] = useState<ChatMessagePayload[]>([]);

  const socketRef = useRef<WebSocket | null>(null);
  // 2. Lifecycle Hook (Runs automatically when the web page loads)
  useEffect(() => {
    const socket = new WebSocket('ws://192.168.1.102:8085/');
    socketRef.current = socket;

    socket.onopen = () => {
      console.log('Successfully connected to SlytaBot Backend!');
    };

    // When a message arrives from your C# app
    socket.onmessage = (event) => {
      // Parse the raw JSON text string back into a typed TypeScript object
      const data: ChatMessagePayload = JSON.parse(event.data);

      if (data.Type === 'ChatMessage') {
        // Append the new message to our list (array)
        setMessages((prev) => [...prev, data]);
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

      <div style={{ border: '1px solid #444', borderRadius: '8px', padding: '15px', height: '400px', overflowY: 'auto' }}>
        {messages.map((msg) => (
          <div key={msg.MessageID} style={{ marginBottom: '10px', fontSize: '16px' }}>
            <strong style={{ color: msg.UserColor }}>{msg.Username}: </strong>
            <span>{msg.Message}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
