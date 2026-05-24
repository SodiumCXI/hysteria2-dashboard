import axios from 'axios'
import { getRouteSalt } from '@/utils/routePrefix'

const API_URL = import.meta.env.DEV ? 'http://localhost:5000' : ''

const apiClient = axios.create({ baseURL: API_URL })

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  const salt = getRouteSalt()
  
  if (salt) {
    config.headers['X-Route-Salt'] = salt
  }

  return config
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && !error.config?.url?.includes('/login')) {
      localStorage.removeItem('token')
      const prefix = '/' + getRouteSalt()
      window.location.href = `${prefix}/login`
    }
    return Promise.reject(error)
  }
)

export default apiClient