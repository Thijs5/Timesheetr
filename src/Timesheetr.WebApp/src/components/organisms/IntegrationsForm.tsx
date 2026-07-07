import { useState, useEffect } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Check, Copy, Eye, EyeOff } from 'lucide-react'
import { SiJira } from 'react-icons/si'
import { TsButton } from '@/components/atoms/TsButton'
import { TsInput } from '@/components/atoms/TsInput'
import { TsLabel } from '@/components/atoms/TsLabel'
import { TsAlert } from '@/components/atoms/TsAlert'
import { TsCard, TsCardContent } from '@/components/atoms/TsCard'
import { IntegrationHeader } from '@/components/molecules/IntegrationHeader'
import { api } from '@/lib/api'
import type { AppSettings } from '@/types'

type SettingsFormState = Omit<AppSettings, 'isConfigured'>

export function IntegrationsForm() {
  const queryClient = useQueryClient()
  const { data: settings, isLoading } = useQuery({ queryKey: ['settings'], queryFn: api.getSettings })

  const [form, setForm] = useState<SettingsFormState>({
    togglApiToken: '',
    tempoApiToken: '',
    jiraAccountId: '',
    jiraBaseUrl: '',
    jiraEmail: '',
    jiraApiToken: '',
  })
  const [saving, setSaving] = useState(false)
  const [saved, setSaved] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [visiblePasswords, setVisiblePasswords] = useState<Partial<Record<keyof SettingsFormState, boolean>>>({})
  const [copiedField, setCopiedField] = useState<keyof SettingsFormState | null>(null)

  useEffect(() => {
    if (settings) {
      const { isConfigured: _, ...rest } = settings
      setForm(rest)
    }
  }, [settings])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setSaving(true)
    setError(null)
    try {
      const res = await api.saveSettings(form)
      if (!res.ok) throw new Error(await res.text())
      await queryClient.invalidateQueries({ queryKey: ['settings'] })
      setSaved(true)
      setTimeout(() => setSaved(false), 2000)
    } catch (err) {
      setError((err as Error).message)
    } finally {
      setSaving(false)
    }
  }

  const togglePasswordVisibility = (key: keyof SettingsFormState) => {
    setVisiblePasswords(prev => ({ ...prev, [key]: !prev[key] }))
  }

  const copyFieldValue = async (key: keyof SettingsFormState) => {
    const value = form[key]
    if (!value) return

    try {
      await navigator.clipboard.writeText(value)
      setCopiedField(key)
      setTimeout(() => setCopiedField(current => (current === key ? null : current)), 1500)
    } catch {
      setError('Failed to copy value to clipboard')
    }
  }

  const field = (key: keyof SettingsFormState, label: string, type = 'text', placeholder = '') => (
    <div className="space-y-1.5">
      <TsLabel htmlFor={key}>{label}</TsLabel>
      <div className="flex items-stretch">
        <div className="relative flex-1">
          <TsInput
            id={key}
            type={type === 'password' && visiblePasswords[key] ? 'text' : type}
            value={form[key]}
            placeholder={placeholder}
            className={`rounded-r-none ${type === 'password' ? 'pr-10' : ''}`}
            onChange={e => setForm(prev => ({ ...prev, [key]: e.target.value }))}
          />
          {type === 'password' && (
            <TsButton
              type="button"
              variant="ghost"
              size="icon"
              className="absolute right-1 top-1/2 h-7 w-7 -translate-y-1/2"
              onClick={() => togglePasswordVisibility(key)}
              aria-label={visiblePasswords[key] ? `Hide ${label}` : `Show ${label}`}
              title={visiblePasswords[key] ? 'Hide value' : 'Show value'}
            >
              {visiblePasswords[key] ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </TsButton>
          )}
        </div>
        <TsButton
          type="button"
          variant="outline"
          size="icon"
          className="rounded-l-none border-l-0"
          onClick={() => copyFieldValue(key)}
          disabled={!form[key]}
          aria-label={`Copy ${label}`}
          title="Copy value"
        >
          {copiedField === key ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
        </TsButton>
      </div>
    </div>
  )

  if (isLoading) {
    return (
      <div className="flex items-center gap-2 text-muted-foreground py-8">
        <Loader2 className="h-4 w-4 animate-spin" /> Loading settings…
      </div>
    )
  }

  return (
    <form onSubmit={handleSubmit} className="max-w-3xl space-y-4">
      <div className="grid gap-4 md:grid-cols-2">
        <TsCard>
          <IntegrationHeader logoSrc="/toggl-favicon.png" color="#e01e37" title="Toggl" description="Time tracking source" />
          <TsCardContent className="space-y-4">
            {field('togglApiToken', 'API Token', 'password', 'Your Toggl Track API token')}
          </TsCardContent>
        </TsCard>

        <TsCard>
          <IntegrationHeader logoSrc="/tempo-favicon.ico" color="#6554c0" title="Tempo" description="Jira time logging" />
          <TsCardContent className="space-y-4">
            {field('tempoApiToken', 'API Token', 'password', 'Your Tempo API token')}
          </TsCardContent>
        </TsCard>

        <TsCard className="md:col-span-2">
          <IntegrationHeader icon={SiJira} color="#0052cc" title="Jira" description="Issue tracking destination" />
          <TsCardContent className="grid gap-4 sm:grid-cols-2">
            {field('jiraBaseUrl', 'Base URL', 'url', 'https://your-org.atlassian.net')}
            {field('jiraEmail', 'Email', 'email', 'you@example.com')}
            {field('jiraApiToken', 'API Token', 'password', 'Your Jira API token')}
            {field('jiraAccountId', 'Account ID', 'text', '63aea1ec...')}
          </TsCardContent>
        </TsCard>
      </div>

      {error && <TsAlert variant="destructive" message={error} />}

      <div className="flex justify-end">
        <TsButton type="submit" disabled={saving}>
          {saving ? (
            <><Loader2 className="h-4 w-4 animate-spin" /> Saving…</>
          ) : saved ? (
            <><Check className="h-4 w-4" /> Saved</>
          ) : (
            'Save integrations'
          )}
        </TsButton>
      </div>
    </form>
  )
}
