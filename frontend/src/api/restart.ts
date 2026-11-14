import apiClient from './client'

export const restartHysteria = async (): Promise<void> => {
  await apiClient.post<void>('/api/restart/hysteria')
}

export const restartDashboard = async (dashboardPort: string, routeSalt: string): Promise<void> => {
  try {
    await apiClient.post<void>('/api/restart/dashboard', { dashboardPort, routeSalt })
  } catch (err: any) {
    if (err?.response) throw err
  }
}

export const waitForDashboard = async (url: string): Promise<boolean> => {
  for (let attempt = 0; attempt < 40; attempt++) {
    try {
      await fetch(url, { mode: 'no-cors', cache: 'no-store' })
      return true
    } catch {
      await new Promise(resolve => setTimeout(resolve, 500))
    }
  }
  return false
}
