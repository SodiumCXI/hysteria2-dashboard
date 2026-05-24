import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';

const API_URL = import.meta.env.DEV ? 'http://localhost:5000' : ''

export interface TrafficDto {
  username: string;
  txBytes: number;
  rxBytes: number;
}

export function useTrafficHub() {
  const [traffic, setTraffic] = useState<TrafficDto[]>([]);

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (!token) return;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/traffic`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveTraffic', (data: TrafficDto[]) => {
      setTraffic(data);
    });

    connection.start().catch(() => {});

    return () => {
      connection.stop();
    };
  }, []);

  return traffic;
}