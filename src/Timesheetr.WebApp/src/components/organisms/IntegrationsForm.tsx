import { useState, useEffect } from 'react'
import { useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Check } from 'lucide-react'
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

  const field = (key: keyof SettingsFormState, label: string, type = 'text', placeholder = '') => (
    <div className="space-y-1.5">
      <TsLabel htmlFor={key}>{label}</TsLabel>
      <TsInput
        id={key}
        type={type}
        value={form[key]}
        placeholder={placeholder}
        onChange={e => setForm(prev => ({ ...prev, [key]: e.target.value }))}
      />
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
