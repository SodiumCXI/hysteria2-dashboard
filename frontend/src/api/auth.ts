import apiClient from './client'

export const login = async (password: string): Promise<string> => {
  const response = await apiClient.post<{ token: string }>('/api/auth/login', { password })
  return response.data.token
}