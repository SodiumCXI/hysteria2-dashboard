import apiClient from './client'

export interface User {
  username: string
  key: string
}

export const getUsers = async (): Promise<User[]> => {
  const response = await apiClient.get<User[]>('/api/users')
  return response.data
}

export const createUser = async (username: string): Promise<User> => {
  const response = await apiClient.post<User>('/api/users', { username })
  return response.data
}

export const deleteUser = async (username: string): Promise<void> => {
  await apiClient.delete(`/api/users/${username}`)
}