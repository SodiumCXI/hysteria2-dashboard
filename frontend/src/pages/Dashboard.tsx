import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Dialog, DialogContent, DialogFooter } from "@/components/ui/dialog"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  Activity, ArrowDownLeft, ArrowUpRight, Copy, RefreshCw,
  LogOut, KeyRound, Plus, UserKey, Settings, Trash2, UserX,
  Cpu, MemoryStick, Percent
} from "lucide-react"
import { getUsers, createUser, deleteUser, type User } from '@/api/users'
import { getSettings, saveSettings, savePassword, type Settings as SettingsType } from '@/api/settings'
import { restartDashboard, restartHysteria, waitForDashboard } from '@/api/restart'
import { useTrafficHub } from '@/hooks/useTrafficHub'
import { useStatusHub } from '@/hooks/useStatusHub'
import { useUsageHub } from '@/hooks/useUsageHub'

function Dashboard() {
  const navigate = useNavigate()
  const { t } = useTranslation()

  const traffic = useTrafficHub()
  const hubStatus = useStatusHub()
  const usage = useUsageHub()
  
  const [status, setStatus] = useState<number | null>(hubStatus)
  const [keys, setKeys] = useState<User[]>([])
  const [copiedName, setCopiedName] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState("traffic")
  const [error, setError] = useState<string | null>(null)

  const [spinning, setSpinning] = useState(false)
  const [createOpen, setCreateOpen] = useState(false)
  const [newName, setNewName] = useState('')
  const [creating, setCreating] = useState(false)
  const [deletingName, setDeletingName] = useState<string | null>(null)

  const [restartDisabled, setRestartDisabled] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [settings, setSettings] = useState<SettingsType>({ hysteriaPort: '', sni: '', obfsPassword: '', keyName: '', dashboardPort: '', routeSalt: '' })
  const [savingSettings, setSavingSettings] = useState(false)
  const [newPassword, setNewPassword] = useState('')
  const [errorOpen, setErrorOpen] = useState(false)

  const isRestartButtonDisabled = restartDisabled || status !== 1;
  
  useEffect(() => {
    loadKeys()
  }, [])

  useEffect(() => {
    setStatus(hubStatus)
  }, [hubStatus])

  async function loadKeys() {
    try {
      const data = await getUsers()
      setKeys(data)
    } catch (err: any) {
      setError(err?.response?.data?.message ?? err?.message ?? 'Unknown error')
      setErrorOpen(true)
    }
  }

  async function loadSettings() {
    try {
      const data = await getSettings()
      setSettings(data)
    }
    catch (err: any) {
      setError(err?.response?.data?.message ?? err?.message ?? 'Unknown error')
    }
  }

  function handleLogout() {
    localStorage.removeItem('token')
    navigate('/login')
  }

  function handleCopyKey(user: User) {
    navigator.clipboard.writeText(user.key)
    setCopiedName(user.username)
    setTimeout(() => setCopiedName(null), 1000)
  }

  async function handleCreateKey() {
    if (!newName.trim()) return
    setCreating(true)
    try {
      const newUser = await createUser(newName.trim())
      setKeys(prev => [...prev, newUser])
      setNewName('')
      setCreateOpen(false)
    }
    catch (err: any) {
      setError(err?.response?.data?.message ?? err?.message ?? 'Unknown error')
    }
    finally { setCreating(false) }
  }

  async function handleDeleteKey(username: string) {
    setDeletingName(username)
    try {
      await deleteUser(username)
      setKeys(prev => prev.filter(k => k.username !== username))
    }
    catch (err: any) {
      setError(err?.response?.data?.message ?? err?.message ?? 'Unknown error')
      setErrorOpen(true)
    }
    finally { setDeletingName(null) }
  }

  async function handleOpenSettings() {
    setError(null)
    setNewPassword('')
    await loadSettings()
    setSettingsOpen(true)
  }

  async function handleSaveSettings() {
    setSavingSettings(true)
    try {
      if (newPassword) {
        const newToken = await savePassword(newPassword)
        localStorage.setItem('token', newToken)
      }
      const result = await saveSettings(settings)
      if (result.newUrl) {
        await restartDashboard(settings.dashboardPort, settings.routeSalt)
        window.location.assign(await waitForDashboard(result.newUrl) ? result.newUrl : window.location.href)
        return
      }
      setSettingsOpen(false)
      await loadKeys()
    }
    catch (err: any) {
      if (err?.response?.status === 401) return
      setError(err?.response?.data?.message ?? err?.message ?? 'Unknown error')
    }
    finally { setSavingSettings(false) }
  }


  async function handleRestartHysteria() {
    setSpinning(true)
    setRestartDisabled(true)
    setTimeout(() => setSpinning(false), 500)
    setTimeout(() => setRestartDisabled(false), 5000)
    try {
      await restartHysteria()
      setStatus(2)
    }
    catch (err: any) {
      setError(err?.response?.data?.message ?? err?.message ?? 'Unknown error')
    }
  }

  function generatePassword(length: number) {
    const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
    const array = new Uint8Array(length)
    crypto.getRandomValues(array)
    return Array.from(array, b => chars[b % chars.length]).join('')
  }

  const totalTx = traffic.reduce((sum, t) => sum + t.txBytes, 0)
  const totalRx = traffic.reduce((sum, t) => sum + t.rxBytes, 0)

  function formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
    if (bytes < 1024 * 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`
    return `${(bytes / (1024 * 1024 * 1024 * 1024)).toFixed(1)} TB`
  }

  const shellStyle = "border border-white/5 bg-[#11151c]/90 shadow-md shadow-black/40"
  const rowStyle = "border-b border-white/5 bg-[#11151c]/90 transition-colors"
  const headStyle = "text-zinc-400 uppercase tracking-wide text-[11px] font-medium"

  function getUsageColor(value: number) {
    if (value < 50) return { text: 'text-emerald-400', bar: 'bg-emerald-400/60', icon: 'text-emerald-400' }
    if (value < 80) return { text: 'text-yellow-500', bar: 'bg-yellow-500/60', icon: 'text-yellow-500' }
    return { text: 'text-red-400', bar: 'bg-red-400/60', icon: 'text-red-400' }
  }

  const statusColor: Record<number, string> = {
    0: 'text-emerald-400',
    1: 'text-red-400',
    2: 'text-yellow-600'
  }

  const statusLabels: Record<number, string> = {
    0: t('statusRunning'),
    1: t('statusStopped'),
    2: t('statusStarting')
  }

  return (
    <div className={`min-h-screen text-white transition-[background] duration-500 ${
      activeTab === "keys"
        ? "bg-[radial-gradient(circle_at_top,_rgba(56,189,248,0.16),_transparent_35%),linear-gradient(to_bottom_right,_#09090b,_#10131a_50%,_#09090b)]"
        : "bg-[radial-gradient(circle_at_top,_rgba(48,205,158,0.16),_transparent_35%),linear-gradient(to_bottom_right,_#09090b,_#10131a_50%,_#09090b)]"
    }`}>
      <div className="mx-auto max-w-7xl px-4 py-6 md:px-6 md:py-8">

        {/* Header */}
        <div className={`${shellStyle} rounded-3xl p-5 md:p-6`}>
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap items-center gap-3">
              <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-sky-500/15 text-sky-300 ring-1 ring-sky-500/20">
                <UserKey className="h-6 w-6 mt-0.5 ml-0.5" />
              </div>
              <h1 className="pl-2 text-2xl tracking-tight md:text-3xl">{t('title')}</h1>
            </div>

            {/* System resources */}
            <div className="flex flex-1 gap-3 lg:mx-6">
              <div className="flex h-11 flex-1 items-center gap-3 rounded-2xl border border-white/5 bg-[#11151c]/90 px-4">
                <div className={getUsageColor(usage?.cpuUsage ?? 0).icon}>
                  <Cpu className="h-4 w-4" />
                </div>
                <span className="text-sm text-zinc-400 mb-[1px]">CPU</span>
                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-white/[0.06]">
                  <div className={`h-full rounded-full transition-all duration-700 ${getUsageColor(usage?.cpuUsage ?? 0).bar}`} style={{ width: `${usage?.cpuUsage ?? 0}%` }} />
                </div>
                <span className={`flex w-10 items-center justify-end gap-0.5 font-mono text-sm tabular-nums ${getUsageColor(usage?.cpuUsage ?? 0).text}`}>
                  {usage?.cpuUsage ?? 0}<Percent size={13} strokeWidth={2.5} />
                </span>
              </div>
              <div className="flex h-11 flex-1 items-center gap-3 rounded-2xl border border-white/5 bg-[#11151c]/90 px-4">
                <div className={getUsageColor(usage?.ramUsage ?? 0).icon}>
                  <MemoryStick className="h-4 w-4" />
                </div>
                <span className="text-sm text-zinc-400 mb-[1px]">RAM</span>
                <div className="h-1.5 flex-1 overflow-hidden rounded-full bg-white/[0.06]">
                  <div className={`h-full rounded-full transition-all duration-700 ${getUsageColor(usage?.ramUsage ?? 0).bar}`} style={{ width: `${usage?.ramUsage ?? 0}%` }} />
                </div>
                <span className={`flex w-10 items-center justify-end gap-0.5 font-mono text-sm tabular-nums ${getUsageColor(usage?.ramUsage ?? 0).text}`}>
                  {usage?.ramUsage ?? 0}<Percent size={13} strokeWidth={2.5} />
                </span>
              </div>
            </div>

            <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
              <Button variant="outline" onClick={handleOpenSettings}
                className="h-11 cursor-pointer rounded-2xl border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white">
                <Settings className="mr-1 h-4 w-4" />
                <span>{t('settings')}</span>
              </Button>
              <Button variant="ghost" onClick={handleLogout}
                className="h-11 cursor-pointer rounded-2xl border-rose-500/20 bg-rose-500/10 text-rose-300 hover:bg-rose-500/20 hover:text-rose-200">
                <LogOut className="mr-1 h-4 w-4" />
                <span>{t('logout')}</span>
              </Button>
            </div>
          </div>
        </div>

        {/* Metrics */}
        <div className="mt-6 grid gap-4 md:grid-cols-3">
          <Card className={shellStyle}>
            <CardContent className="pl-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-zinc-400">{t('download')}</p>
                  <p className="mt-3 text-2xl">{formatBytes(totalRx)}</p>
                </div>
                <div className="rounded-2xl bg-white/[0.03] p-3 text-sky-400 ring-1 ring-white/6">
                  <ArrowDownLeft className="h-5 w-5" />
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className={shellStyle}>
            <CardContent className="pl-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-zinc-400">{t('upload')}</p>
                  <p className="mt-3 text-2xl">{formatBytes(totalTx)}</p>
                </div>
                <div className="rounded-2xl bg-white/[0.03] p-3 text-violet-400 ring-1 ring-white/6">
                  <ArrowUpRight className="h-5 w-5" />
                </div>
              </div>
            </CardContent>
          </Card>
          <Card className={shellStyle}>
            <CardContent className="pl-5">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-zinc-400">{t('status')}</p>
                  <div className="flex items-center gap-2">
                    <p className="mt-2.5 text-2xl">
                      {status !== null ? statusLabels[status] ?? t('statusUnknown') : '—'}
                    </p>
                    <Button style={{
                      opacity: status === 1 ? isRestartButtonDisabled ? 0.5 : 1 : 0,
                      transition: 'opacity 0.5s'
                      }}
                      variant="outline" onClick={handleRestartHysteria} disabled={isRestartButtonDisabled}
                      className="mt-2.5 h-8 w-8 cursor-pointer rounded-xl border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white">
                      <RefreshCw className={`h-4 w-4 ${spinning ? 'transition-transform duration-500 rotate-180' : 'rotate-0'}`} />
                    </Button>
                  </div>
                </div>
                <div className={`mb-0.5 rounded-2xl bg-white/[0.03] p-3 ring-1 ring-white/6 ${status !== null ? statusColor[status] : 'text-zinc-400'}`}>
                  <Activity className="h-5 w-5" />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="mt-6">
          <div className="mt-2 flex items-center justify-between gap-4 mx-auto">
            <TabsList className="h-11 rounded-2xl border border-white/8 bg-[#11151c]/90 pt-5 pb-5 shadow-md shadow-black/40">
              <TabsTrigger value="traffic"
                className="rounded-xl p-4 text-sm font-medium text-zinc-400 hover:text-emerald-200 transition-all data-active:bg-emerald-500/15 data-active:text-emerald-200">
                <span>{t('traffic')}</span>
              </TabsTrigger>
              <TabsTrigger value="keys"
                className="rounded-xl p-4 text-sm font-medium text-zinc-400 hover:text-sky-200 transition-all data-active:bg-sky-500/15 data-active:text-sky-200">
                <span>{t('keys')}</span>
              </TabsTrigger>
            </TabsList>
          </div>

          {/* Traffic */}
          <TabsContent value="traffic" className="mt-6">
            {traffic.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-white/8 bg-white/[0.01] p-10 text-center">
                <p className="text-zinc-300">{t('noConnections')}</p>
                <p className="mt-2 text-sm text-zinc-500">{t('trafficUpdates')}</p>
              </div>
            ) : (
              <div className={`${shellStyle} overflow-hidden rounded-2xl`}>
                <Table>
                  <TableHeader className="bg-white/[0.01]">
                    <TableRow className="border-b border-white/5 hover:bg-transparent">
                      <TableHead className={`${headStyle} pl-5`}>
                        <p>{t('name')}</p>
                      </TableHead>
                      <TableHead className={headStyle}>
                        <p>{t('download')} ↓</p>
                      </TableHead>
                      <TableHead className={headStyle}>
                        <p>{t('upload')} ↑</p>
                      </TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {traffic.map((entry) => {
                      const maxTx = Math.max(...traffic.map(t => t.txBytes))
                      const maxRx = Math.max(...traffic.map(t => t.rxBytes))
                      const upPct = maxTx > 0 ? (entry.txBytes / maxTx) * 100 : 0
                      const downPct = maxRx > 0 ? (entry.rxBytes / maxRx) * 100 : 0
                      return (
                        <TableRow key={entry.username} className="border-b border-white/5 transition-colors">
                          <TableCell className="py-3.5">
                            <div className="h-8 px-3 inline-flex items-center rounded-xl bg-white/[0.06] font-mono text-sm text-white ring-1 ring-white/8">
                              <p>{entry.username}</p>
                            </div>
                          </TableCell>
                          <TableCell className="py-3.5">
                            <div className="flex flex-col gap-2">
                              <span className="font-mono text-sm font-medium text-sky-300">{formatBytes(entry.rxBytes)}</span>
                              <div className="h-1 w-32 overflow-hidden rounded-full bg-white/[0.06]">
                                <div className="h-full rounded-full bg-sky-400/60 transition-all duration-700" style={{ width: `${downPct}%` }} />
                              </div>
                            </div>
                          </TableCell>
                          <TableCell className="py-3.5">
                            <div className="flex flex-col gap-2">
                              <span className="font-mono text-sm font-medium text-violet-300">{formatBytes(entry.txBytes)}</span>
                              <div className="h-1 w-32 overflow-hidden rounded-full bg-white/[0.06]">
                                <div className="h-full rounded-full bg-violet-400/60 transition-all duration-700" style={{ width: `${upPct}%` }} />
                              </div>
                            </div>
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
              </div>
            )}
          </TabsContent>

          {/* Keys */}
          <TabsContent value="keys" className="relative mt-6">
            <div className="relative">
              <Button className="group absolute -top-14 right-0 h-11 cursor-pointer rounded-2xl border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white"
                onClick={() => {setError(null); setCreateOpen(true)}}>
                <Plus className="mr-1 h-4 w-4 transition-transform duration-250 group-hover:rotate-90" />
                {t('createKey')}
              </Button>
              <div className={`${shellStyle} overflow-hidden rounded-2xl`}>
                <Table>
                  <TableHeader className="bg-white/[0.01]">
                    <TableRow className="border-b border-white/5 hover:bg-transparent">
                      <TableHead className={`${headStyle} pl-5`}>
                        <p>{t('name')}</p>
                      </TableHead>
                      <TableHead className={`${headStyle} pl-5`}>
                        <p>{t('key')}</p>
                      </TableHead>
                      <TableHead className={`${headStyle} text-right`} />
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {keys.map((key) => (
                      <TableRow key={key.username} className={rowStyle}>
                        <TableCell className="py-3 whitespace-nowrap">
                          <div className="h-8 px-3 inline-flex items-center rounded-xl bg-white/[0.06] font-mono text-sm text-white ring-1 ring-white/8">
                            <p>{key.username}</p>
                          </div>
                        </TableCell>
                        <TableCell className="py-2 max-w-[500px]">
                          <button
                            onClick={() => handleCopyKey(key)}
                            className="group relative flex w-full items-center gap-3 rounded-xl px-3 py-2 text-left font-mono text-xs text-zinc-400 transition-colors hover:bg-white/[0.04] hover:text-sky-300"
                            title="Click to copy">
                            <Copy className="h-3.5 w-3.5 shrink-0 opacity-60 transition group-hover:opacity-100" />
                            <span className={copiedName === key.username ? "opacity-0 break-words whitespace-normal" : "break-words whitespace-normal"}>
                              {key.key}
                            </span>
                            {copiedName === key.username && (
                              <span className="absolute left-10 top-1/2 -translate-y-1/2 text-emerald-300">
                                {t('copied')}
                              </span>
                            )}
                          </button>
                        </TableCell>
                        <TableCell className="py-3 text-right whitespace-nowrap">
                          <Button size="sm"
                            disabled={deletingName === key.username}
                            className="h-8.5 cursor-pointer rounded-xl border border-rose-500/15 bg-rose-500/10 text-rose-300 hover:bg-rose-500/20 hover:text-rose-200"
                            onClick={() => handleDeleteKey(key.username)}>
                            <Trash2 className="mr-1 h-4 w-4" />
                            <p>{deletingName === key.username ? t('deleting') : t('delete')}</p>
                          </Button>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </div>
          </TabsContent>

        </Tabs>

        {/* Create dialog */}
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogContent className="rounded-2xl border border-white/5 bg-[#11151c]/95 p-6 text-white shadow-none sm:max-w-md">
            <div className="grid gap-4">
              <div className="grid gap-2">
                <label className="px-1 text-sm text-zinc-400">{t('keyName')}</label>
                <Input
                  placeholder={t('enterUsername')}
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleCreateKey()}
                  className="h-9 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                />
              </div>
            </div>
            {error && (
              <p className="-mb-3 mt-0.5 text-sm text-center text-rose-400/80">{error}</p>
            )}
            <DialogFooter className="gap-2 sm:gap-0">
              <Button variant="ghost"
                className="h-9 mr-1 rounded-xl cursor-pointer border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white"
                onClick={() => setCreateOpen(false)}>
                <span>{t('cancel')}</span>
              </Button>
              <Button disabled={creating}
                className="h-9 rounded-xl border-sky-500/20 cursor-pointer bg-sky-500/15 text-sky-300 hover:bg-sky-500/20 hover:text-sky-200"
                onClick={handleCreateKey}>
                <span>{creating ? t('creating') : t('create')}</span>
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Settings dialog */}
        <Dialog open={settingsOpen} onOpenChange={setSettingsOpen}>
          <DialogContent className="rounded-2xl border border-white/5 bg-[#11151c]/95 p-6 text-white shadow-none sm:max-w-3xl">
            <div className="flex gap-6">
              {/* Left column */}
              <div className="flex flex-1 flex-col gap-4">
                <div className="grid gap-4 md:grid-cols-2">
                  <div className="space-y-2">
                    <label className="px-1 text-sm text-zinc-400">{t('sni')}</label>
                    <Input value={settings.sni}
                      onChange={(e) => setSettings(prev => ({ ...prev, sni: e.target.value }))}
                      className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                    />
                  </div>
                  <div className="space-y-2">
                    <label className="px-1 text-sm text-zinc-400">{t('port')}</label>
                    <Input value={settings.hysteriaPort}
                      onChange={(e) => setSettings(prev => ({ ...prev, hysteriaPort: e.target.value }))}
                      className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                    />
                  </div>
                </div>
                <div className="space-y-2">
                  <label className="px-1 text-sm text-zinc-400">{t('settingsKeyName')}</label>
                  <Input value={settings.keyName}
                    onChange={(e) => setSettings(prev => ({ ...prev, keyName: e.target.value }))}
                    className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                  />
                </div>
                <div className="space-y-2">
                  <label className="px-1 text-sm text-zinc-400">{t('obfsPassword')}</label>
                  <div className="relative mt-2">
                    <Input type="text"
                      placeholder={t('obfsPassword')}
                      value={settings.obfsPassword}
                      onChange={(e) => setSettings(prev => ({ ...prev, obfsPassword: e.target.value }))}
                      className="h-9 pr-14 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                    />
                    <div className="absolute right-0 top-1/2 flex h-6 -translate-y-1/2 items-center">
                      <div className="h-5 w-px bg-white/10" />
                      <Button type="button" variant="ghost"
                        onClick={() => setSettings(prev => ({ ...prev, obfsPassword: generatePassword(32) }))}
                        className="h-9 cursor-pointer rounded-none rounded-r-2xl px-3 text-zinc-200 hover:bg-white/[0.06] hover:text-white">
                        <KeyRound className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
              </div>

              {/* Divider */}
              <div className="w-px bg-white/8 self-stretch" />

              {/* Right column */}
              <div className="flex w-66 flex-col gap-4">
                <div className="space-y-2">
                  <label className="px-1 text-sm text-zinc-400">{t('dashboardPort')}</label>
                  <Input value={settings.dashboardPort}
                    onChange={(e) => setSettings(prev => ({ ...prev, dashboardPort: e.target.value }))}
                    className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                  />
                </div>
                <div className="space-y-2">
                  <label className="px-1 text-sm text-zinc-400">{t('routeSalt')}</label>
                  <div className="relative mt-2">
                    <Input value={settings.routeSalt}
                      onChange={(e) => setSettings(prev => ({ ...prev, routeSalt: e.target.value }))}
                      className="h-9 pr-14 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                    />
                    <div className="absolute right-0 top-1/2 flex h-6 -translate-y-1/2 items-center">
                      <div className="h-5 w-px bg-white/10" />
                      <Button type="button" variant="ghost"
                        onClick={() => setSettings(prev => ({ ...prev, routeSalt: generatePassword(16) }))}
                        className="h-9 cursor-pointer rounded-none rounded-r-2xl px-3 text-zinc-200 hover:bg-white/[0.06] hover:text-white">
                        <KeyRound className="h-4 w-4" />
                      </Button>
                    </div>
                  </div>
                </div>
                <div className="space-y-2">
                  <label className="px-1 text-sm text-zinc-400">{t('newPassword')}</label>
                  <Input value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                  />
                </div>
              </div>
            </div>

            {error && (
              <p className="-mb-3 mt-0.5 text-sm text-center text-rose-400/80">{error}</p>
            )}
            <DialogFooter className="gap-2 sm:gap-0">
              <span className="mr-auto ml-1 flex items-center px-1 text-lg text-zinc-600">
                v1.3.0
              </span>
              <Button variant="ghost"
                className="h-9 mr-1 rounded-xl cursor-pointer border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white"
                onClick={() => setSettingsOpen(false)}>
                {t('cancel')}
              </Button>
              <Button disabled={savingSettings}
                className="h-9 rounded-xl border-sky-500/20 cursor-pointer bg-sky-500/15 text-sky-300 hover:bg-sky-500/20 hover:text-sky-200"
                onClick={handleSaveSettings}>
                <span>{savingSettings ? t('saving') : t('save')}</span>
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        <Dialog open={errorOpen} onOpenChange={setErrorOpen}>
          <DialogContent className="rounded-2xl border border-white/5 bg-[#11151c]/95 p-6 text-white shadow-none sm:max-w-md">
            <div className="flex flex-col items-center gap-4">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-rose-500/10">
                <UserX className="mt-0.5 ml-0.5 text-rose-400/90" size={28} strokeWidth={1.5} />
              </div>
              <p className="text-sm text-center text-rose-400/80">{error}</p>
            </div>
          </DialogContent>
        </Dialog>

      </div>
    </div>
  )
}

export default Dashboard