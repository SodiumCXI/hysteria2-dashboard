import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { getRouteSalt } from '@/utils/routePrefix'

const API_URL = import.meta.env.DEV ? 'http://localhost:5000' : ''

export function useStatusHub() {
  const [status, setStatus] = useState<number | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/status?salt=${getRouteSalt()}`, {
        accessTokenFactory: () => localStorage.getItem('token') ?? ''
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveStatus', (data: number) => {
      setStatus(data);
    });

    connection.start().catch(() => {});

    return () => {
      connection.stop();
    };
  }, []);

  return status;
}
