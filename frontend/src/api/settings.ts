import apiClient from './client'

export interface Settings {
  port: string
  sni: string
  obfsPassword: string
  keyName: string
}

export const getSettings = async (): Promise<Settings> => {
  const response = await apiClient.get<Settings>('/api/settings')
  return response.data
}

export const saveSettings = async (settings: Settings): Promise<void> => {
  await apiClient.put('/api/settings', settings)
}