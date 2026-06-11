import { useTranslation } from 'react-i18next'

const LANGS = [
  { code: 'ru', label: 'Русский' },
  { code: 'en', label: 'English' },
  { code: 'zh', label: '中文' }
]

export function LanguageSwitcher({ className = '' }: { className?: string }) {
  const { i18n } = useTranslation()
  const current = i18n.language?.slice(0, 2)

  return (
    <div className={`flex gap-1 rounded-2xl ${className}`}>
      {LANGS.map(({ code, label }) => (
        <button
          key={code}
          onClick={() => i18n.changeLanguage(code)}
          className={`px-3 py-2 rounded-xl text-sm font-medium transition-colors cursor-pointer
            ${current === code
              ? 'bg-sky-500/15 text-sky-200'
              : 'text-zinc-400 hover:text-zinc-200 hover:bg-white/[0.04]'
            }`}
        >
          {label}
        </button>
      ))}
    </div>
  )
}
