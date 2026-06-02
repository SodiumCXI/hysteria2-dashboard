import apiClient from './client'

export const restartHysteria = async (): Promise<void> => {
  await apiClient.post<void>('/api/hysteria/restart')
}