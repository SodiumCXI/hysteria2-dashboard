import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { login } from '@/api/auth'
import { useTranslation } from 'react-i18next'
import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'

function Login() {
  const { t } = useTranslation()
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)
  const navigate = useNavigate()

  async function handleLogin() {
    if (!password.trim()) return
    setLoading(true)
    setError('')
    try {
      const token = await login(password)

      localStorage.setItem('token', token)
      navigate('/')
    } catch {
      setError(t('invalidPassword'))
    } finally {
      setLoading(false)
    }
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Enter') handleLogin()
  }

  return (
    <div className="min-h-screen text-white bg-[radial-gradient(circle_at_left,_rgba(48,205,158,0.16),_transparent_30%),radial-gradient(circle_at_right,_rgba(56,189,248,0.16),_transparent_30%),linear-gradient(to_bottom_right,_#09090b,_#10131a_50%,_#09090b)]">
      <div className="flex min-h-screen items-center justify-center">
        <Card className="w-full max-w-md rounded-3xl border border-white/5 bg-[#11151c]/90 shadow-lg shadow-black/40">
          <CardHeader className="space-y-4 py-1">
            <CardTitle className="text-center text-2xl font-semibold tracking-tight text-white">
              {t('title')}
            </CardTitle>
          </CardHeader>
          <CardContent className="flex flex-col gap-4">
            <Input
              type="password"
              placeholder={t('enterPassword')}
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              onKeyDown={handleKeyDown}
              disabled={loading}
              className="h-11 rounded-2xl border-white/8 bg-[#11151c] text-white placeholder:text-zinc-500 focus-visible:ring-1 focus-visible:ring-sky-500/60"
            />
            <Button
              onClick={handleLogin}
              disabled={loading}
              className="h-11 rounded-2xl cursor-pointer bg-gradient-to-r from-emerald-400/20 to-sky-500/20 text-white hover:from-emerald-400/30 hover:to-sky-500/30"
            >
              {loading ? t('signingIn') : t('signIn')}
            </Button>
            {error && (
              <p className="text-sm text-center text-rose-400/80">{error}</p>
            )}
            <div className="flex justify-end">
              <LanguageSwitcher />
            </div>
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default Login
