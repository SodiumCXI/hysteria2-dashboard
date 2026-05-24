import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Dialog, DialogContent, DialogFooter } from "@/components/ui/dialog"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  Activity, ArrowDownLeft, ArrowUpRight, Copy,
  LogOut, KeyRound, Plus, UserKey, Settings, Trash2,
} from "lucide-react"
import { getUsers, createUser, deleteUser, type User } from '@/api/users'
import { getSettings, saveSettings, type Settings as SettingsType } from '@/api/settings'
import { useTrafficHub } from '@/hooks/trafficHub'

function Dashboard() {
  const navigate = useNavigate()

  // --- State ---
  const traffic = useTrafficHub()
  const [keys, setKeys] = useState<User[]>([])
  const [copiedName, setCopiedName] = useState<string | null>(null)
  const [activeTab, setActiveTab] = useState("traffic")
  const [isRunning] = useState(true)

  // Создание ключа
  const [createOpen, setCreateOpen] = useState(false)
  const [newName, setNewName] = useState('')
  const [creating, setCreating] = useState(false)

  // Настройки
  const [settingsOpen, setSettingsOpen] = useState(false)
  const [settings, setSettings] = useState<SettingsType>({ port: '', sni: '', obfsPassword: '', keyName: '' })
  const [savingSettings, setSavingSettings] = useState(false)

  // --- Загрузка ключей при монтировании ---
  useEffect(() => {
    loadKeys()
  }, [])

  // --- API функции ---
  async function loadKeys() {
    try {
      const data = await getUsers()
      setKeys(data)
    } catch {
      // Если 401 — перехватчик в client.ts сам редиректнет на логин
    }
  }

  async function loadSettings() {
    try {
      const data = await getSettings()
      setSettings(data)
    } catch { /* ignore */ }
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
    } catch { /* ignore */ }
    finally { setCreating(false) }
  }

  async function handleDeleteKey(username: string) {
    try {
      await deleteUser(username)
      setKeys(prev => prev.filter(k => k.username !== username))
    } catch { /* ignore */ }
  }

  async function handleOpenSettings() {
    await loadSettings()
    setSettingsOpen(true)
  }

  async function handleSaveSettings() {
    setSavingSettings(true)
    try {
      await saveSettings(settings)
      setSettingsOpen(false)
      await loadKeys()
    } catch { /* ignore */ }
    finally { setSavingSettings(false) }
  }

  function generatePassword() {
    const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789"
    const array = new Uint8Array(32)
    crypto.getRandomValues(array)
    return Array.from(array, b => chars[b % chars.length]).join('')
  }

  // Считаем суммарный трафик для карточек метрик
  const totalTx = traffic.reduce((sum, t) => sum + t.txBytes, 0)
  const totalRx = traffic.reduce((sum, t) => sum + t.rxBytes, 0)

  function formatBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
    if (bytes < 1024 * 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024 * 1024)).toFixed(1)} GB`
    return `${(bytes / (1024 * 1024 * 1024 * 1024)).toFixed(1)} TB`
  }

  const shellStyle = "border border-white/5 bg-[#11151c]/90 shadow-none"
  const rowStyle = "border-b border-white/5 transition-colors"
  const headStyle = "text-zinc-400 uppercase tracking-wide text-[11px] font-medium"

  return (
    <div className={`min-h-screen text-white transition-[background] duration-500 ${
      activeTab === "keys"
        ? "bg-[radial-gradient(circle_at_top,_rgba(56,189,248,0.16),_transparent_30%),linear-gradient(to_bottom_right,_#09090b,_#10131a_50%,_#09090b)]"
        : "bg-[radial-gradient(circle_at_top,_rgba(48,205,158,0.16),_transparent_30%),linear-gradient(to_bottom_right,_#09090b,_#10131a_50%,_#09090b)]"
    }`}>
      <div className="mx-auto max-w-7xl px-4 py-6 md:px-6 md:py-8">

        {/* Header */}
        <div className={`${shellStyle} rounded-3xl p-5 md:p-6`}>
          <div className="flex flex-col gap-5 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex flex-wrap items-center gap-3">
              <div className="flex h-11 w-11 items-center justify-center rounded-2xl bg-sky-500/15 text-sky-400 ring-1 ring-sky-500/20">
                <UserKey className="h-6 w-6 mt-0.5" />
              </div>
              <h1 className="pl-2 text-2xl font-semibold tracking-tight md:text-3xl">Hysteria2 Dashboard</h1>
            </div>
            <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
              <Button variant="outline" onClick={handleOpenSettings}
                className="h-11 cursor-pointer rounded-2xl border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white">
                <Settings className="mr-2 h-4 w-4" />Settings
              </Button>
              <Button variant="ghost" onClick={handleLogout}
                className="h-11 cursor-pointer rounded-2xl border-rose-500/20 bg-rose-500/10 text-rose-300 hover:bg-rose-500/20 hover:text-rose-200">
                <LogOut className="mr-2 h-4 w-4" />Log out
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
                  <p className="text-sm text-zinc-400">Download</p>
                  <p className="mt-2 text-2xl font-semibold">{formatBytes(totalRx)}</p>
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
                  <p className="text-sm text-zinc-400">Upload</p>
                  <p className="mt-2 text-2xl font-semibold">{formatBytes(totalTx)}</p>
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
                  <p className="text-sm text-zinc-400">Status</p>
                  <p className="mt-2 text-2xl font-semibold">{isRunning ? 'Active' : 'Paused'}</p>
                </div>
                <div className="rounded-2xl bg-white/[0.03] p-3 text-emerald-300 ring-1 ring-white/6">
                  <Activity className="h-5 w-5" />
                </div>
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Tabs */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="mt-6">
          <div className="flex items-center justify-between gap-4 mx-auto">
            <TabsList className="h-11 rounded-2xl border border-white/8 bg-[#11151c]/90 pt-5 pb-5">
              <TabsTrigger value="traffic"
                className="rounded-xl p-4 text-sm font-medium text-zinc-400 hover:text-emerald-200 transition-all data-active:bg-emerald-500/15 data-active:text-emerald-200">
                Traffic
              </TabsTrigger>
              <TabsTrigger value="keys"
                className="rounded-xl p-4 text-sm font-medium text-zinc-400 hover:text-sky-200 transition-all data-active:bg-sky-500/15 data-active:text-sky-200">
                Keys
              </TabsTrigger>
            </TabsList>
          </div>

          {/* Traffic */}
          <TabsContent value="traffic" className="mt-6">
            <Card className={shellStyle}>
              <CardHeader className="pb-0">
                <CardTitle className="py-2 px-1 text-lg font-semibold">Active Connections</CardTitle>
              </CardHeader>
              <CardContent>
                {traffic.length === 0 ? (
                  <div className="rounded-2xl border border-dashed border-white/8 bg-white/[0.01] p-10 text-center">
                    <p className="text-zinc-300">No active connections</p>
                    <p className="mt-2 text-sm text-zinc-500">Traffic data updates every second</p>
                  </div>
                ) : (
                  <div className="overflow-hidden rounded-2xl border border-white/8">
                    <Table>
                      <TableHeader className="bg-white/[0.01]">
                        <TableRow className="border-b border-white/5 hover:bg-transparent">
                          <TableHead className={`${headStyle} pl-5`}>Name</TableHead>
                          <TableHead className={headStyle}>Download ↓</TableHead>
                          <TableHead className={headStyle}>Upload ↑</TableHead>
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
                              <TableCell className="py-2.5">
                                <div className="inline-flex items-center rounded-xl bg-white/[0.06] px-3 py-1 font-mono text-sm text-white ring-1 ring-white/8">
                                  <span className="mt-0.5">{entry.username}</span>
                                </div>
                              </TableCell>
                              <TableCell className="py-2.5">
                                <div className="flex flex-col gap-2">
                                  <span className="font-mono text-sm font-medium text-sky-300">{formatBytes(entry.rxBytes)}</span>
                                  <div className="h-1 w-32 overflow-hidden rounded-full bg-white/[0.06]">
                                    <div className="h-full rounded-full bg-sky-400/60 transition-all duration-700" style={{ width: `${downPct}%` }} />
                                  </div>
                                </div>
                              </TableCell>
                              <TableCell className="py-2.5">
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
              </CardContent>
            </Card>
          </TabsContent>

          {/* Keys */}
          <TabsContent value="keys" className="mt-6">
            <Card className={shellStyle}>
              <CardHeader className="flex flex-row items-center justify-between space-y-0">
                <CardTitle className="pl-1 text-lg font-semibold">Keys</CardTitle>
                <Button className="h-11 cursor-pointer rounded-2xl border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white" onClick={() => setCreateOpen(true)}>
                  <Plus className="mr-2 h-4 w-4" />Create Key
                </Button>
              </CardHeader>
              <CardContent>
                <div className="overflow-hidden rounded-2xl border border-white/8">
                  <Table>
                    <TableHeader className="bg-white/[0.01]">
                      <TableRow className="border-b border-white/5 hover:bg-transparent">
                        <TableHead className={`${headStyle} pl-5`}>Name</TableHead>
                        <TableHead className={`${headStyle} pl-5`}>Key</TableHead>
                        <TableHead className={`${headStyle} text-right`}> </TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {keys.map((key) => (
                        <TableRow key={key.username} className={rowStyle}>
                          <TableCell className="py-2.5 whitespace-nowrap">
                            <div className="inline-flex items-center rounded-xl bg-white/[0.06] px-3 py-1 font-mono text-sm text-white ring-1 ring-white/8">
                              <span className="mt-0.5">{key.username}</span>
                            </div>
                          </TableCell>
                          <TableCell className="py-2.5 max-w-[680px]">
                            <button
                              onClick={() => handleCopyKey(key)}
                              className="group relative flex w-full items-center gap-3 rounded-xl px-3 py-2 text-left font-mono text-xs text-zinc-400 transition-colors hover:bg-white/[0.04] hover:text-sky-300"
                              title="Click to copy"
                            >
                              <Copy className="h-3.5 w-3.5 shrink-0 opacity-60 transition group-hover:opacity-100" />
                              <span className={copiedName === key.username ? "opacity-0 break-words whitespace-normal" : "break-words whitespace-normal"}>
                                {key.key}
                              </span>
                              {copiedName === key.username && (
                                <span className="absolute left-10 top-1/2 -translate-y-1/2 text-emerald-300">
                                  Copied!
                                </span>
                              )}
                            </button>
                          </TableCell>
                          <TableCell className="py-2.5 text-right whitespace-nowrap">
                            <Button size="sm"
                              className="h-8 cursor-pointer rounded-xl border border-rose-500/15 bg-rose-500/10 text-rose-300 hover:bg-rose-500/20 hover:text-rose-200"
                              onClick={() => handleDeleteKey(key.username)}>
                              <Trash2 className="mr-2 h-4 w-4" />Delete
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </CardContent>
            </Card>
          </TabsContent>
        </Tabs>

        {/* Create dialog */}
        <Dialog open={createOpen} onOpenChange={setCreateOpen}>
          <DialogContent className="rounded-2xl border border-white/5 bg-[#11151c]/95 p-6 text-white shadow-none sm:max-w-md">
            <div className="grid gap-4">
              <div className="grid gap-2">
                <label className="px-1 text-sm text-zinc-400">Name</label>
                <Input
                  placeholder="Enter key name"
                  value={newName}
                  onChange={(e) => setNewName(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleCreateKey()}
                  className="h-9 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                />
              </div>
            </div>
            <DialogFooter className="gap-2 sm:gap-0">
              <Button variant="ghost"
                      className="h-9 mr-1 rounded-xl cursor-pointer border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white"
                      onClick={() => setCreateOpen(false)}>
                Cancel
              </Button>
              <Button disabled={creating}
                      className="h-9 rounded-xl border-sky-500/20 cursor-pointer bg-sky-500/15 text-sky-300 hover:bg-sky-500/20 hover:text-sky-200"
                      onClick={handleCreateKey}>
                {creating ? 'Creating...' : 'Create'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

        {/* Settings dialog */}
        <Dialog open={settingsOpen} onOpenChange={setSettingsOpen}>
          <DialogContent className="rounded-2xl border border-white/5 bg-[#11151c]/95 p-6 text-white shadow-none sm:max-w-md">
            <div className="grid gap-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <label className="px-1 text-sm text-zinc-400">SNI</label>
                  <Input value={settings.sni}
                         onChange={(e) => setSettings(prev => ({ ...prev, sni: e.target.value }))}
                         className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                  />
                </div>
                <div className="space-y-2">
                  <label className="px-1 text-sm text-zinc-400">Port</label>
                  <Input value={settings.port}
                         onChange={(e) => setSettings(prev => ({ ...prev, port: e.target.value }))}
                         className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                  />
                </div>
              </div>
              <div className="space-y-2">
                <label className="px-1 text-sm text-zinc-400">Key Name</label>
                <Input value={settings.keyName}
                       onChange={(e) => setSettings(prev => ({ ...prev, keyName: e.target.value }))}
                       className="h-9 mt-2 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                />
              </div>
              <div className="space-y-2">
                <label className="px-1 text-sm text-zinc-400">Obfs Password</label>
                <div className="relative mt-2">
                  <Input type="text" placeholder="Obfs Password"
                         value={settings.obfsPassword}
                         onChange={(e) => setSettings(prev => ({ ...prev, obfsPassword: e.target.value }))}
                         className="h-9 pr-14 rounded-2xl border-white/8 bg-white/[0.03] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
                  />
                  <div className="absolute right-0 top-1/2 flex h-6 -translate-y-1/2 items-center">
                    <div className="h-5 w-px bg-white/10" />
                    <Button type="button" variant="ghost"
                            onClick={() => setSettings(prev => ({ ...prev, obfsPassword: generatePassword() }))}
                            className="h-9 cursor-pointer rounded-none rounded-r-2xl px-3 text-zinc-200 hover:bg-white/[0.06] hover:text-white">
                      <KeyRound className="h-4 w-4" />
                    </Button>
                  </div>
                </div>
              </div>
            </div>
            <DialogFooter className="gap-2 sm:gap-0">
              <span className="mr-auto ml-1 flex items-center px-1 text-lg text-zinc-600">
                v1.0.0
              </span>
              <Button variant="ghost"
                      className="h-9 mr-1 rounded-xl cursor-pointer border-white/8 bg-white/[0.03] text-zinc-200 hover:bg-white/[0.06] hover:text-white"
                      onClick={() => setSettingsOpen(false)}>
                Cancel
              </Button>
              <Button disabled={savingSettings}
                      className="h-9 rounded-xl border-sky-500/20 cursor-pointer bg-sky-500/15 text-sky-300 hover:bg-sky-500/20 hover:text-sky-200"
                      onClick={handleSaveSettings}>
                {savingSettings ? 'Saving...' : 'Save'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>

      </div>
    </div>
  )
}

export default Dashboard