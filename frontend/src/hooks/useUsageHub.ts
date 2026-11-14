import { useState, useEffect } from 'react';
import * as signalR from '@microsoft/signalr';
import { getRouteSalt } from '@/utils/routePrefix'

const API_URL = import.meta.env.DEV ? 'http://localhost:5000' : ''

export interface UsageDto {
  cpuUsage: number
  ramUsage: number
}

export function useUsageHub() {
  const [usage, setUsage] = useState<UsageDto | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_URL}/hubs/usage?salt=${getRouteSalt()}`, {
        accessTokenFactory: () => localStorage.getItem('token') ?? ''
      })
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveUsage', (data: UsageDto) => {
      setUsage(data);
    });

    connection.start().catch(() => {});

    return () => {
      connection.stop();
    };
  }, []);

  return usage;
}