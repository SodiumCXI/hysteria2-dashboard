import apiClient from './client'

export interface Settings {
  hysteriaPort: string
  sni: string
  obfsPassword: string
  keyName: string
  dashboardPort: string
  routeSalt: string
}

export const getSettings = async (): Promise<Settings> => {
  const response = await apiClient.get<Settings>('/api/settings')
  return response.data
}

export const saveSettings = async (settings: Settings): Promise<{ newUrl?: string }> => {
  const response = await apiClient.put<{ newUrl?: string }>('/api/settings', settings)
  return response.data
}

export const savePassword = async (password: string): Promise<string> => {
  const response = await apiClient.put<{ newToken: string }>('/api/settings/password', { password })
  return response.data.newToken
}