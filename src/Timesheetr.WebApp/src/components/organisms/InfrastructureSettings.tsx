import { useState } from 'react'
import { ExternalLink, Copy, Check, Eye, EyeOff } from 'lucide-react'
import { SiRabbitmq } from 'react-icons/si'
import { TsButton } from '@/components/atoms/TsButton'
import { TsInput } from '@/components/atoms/TsInput'
import { TsLabel } from '@/components/atoms/TsLabel'
import { TsCard, TsCardContent } from '@/components/atoms/TsCard'
import { IntegrationHeader } from '@/components/molecules/IntegrationHeader'

const rabbitMqManagementUrl = import.meta.env.VITE_RABBITMQ_MANAGEMENT_URL as string | undefined
const rabbitMqUsername = import.meta.env.VITE_RABBITMQ_USERNAME as string | undefined
const rabbitMqPassword = import.meta.env.VITE_RABBITMQ_PASSWORD as string | undefined

export function InfrastructureSettings() {
  const [showPassword, setShowPassword] = useState(false)
  const [copiedField, setCopiedField] = useState<'username' | 'password' | null>(null)

  if (!rabbitMqManagementUrl) {
    return <p className="text-sm text-muted-foreground">No infrastructure services configured.</p>
  }

  const copyValue = async (field: 'username' | 'password', value: string | undefined) => {
    if (!value) return

    await navigator.clipboard.writeText(value)
    setCopiedField(field)
    setTimeout(() => setCopiedField(current => (current === field ? null : current)), 1500)
  }

  return (
    <div className="max-w-3xl">
      <TsCard>
        <IntegrationHeader icon={SiRabbitmq} color="#ff6600" title="RabbitMQ" description="Message broker dashboard" />
        <TsCardContent className="space-y-4">
          <a
            href={rabbitMqManagementUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 text-sm font-medium text-primary hover:underline"
          >
            <ExternalLink className="h-4 w-4" />
            Open management dashboard
          </a>
          <div className="grid grid-cols-2 gap-4 max-w-xs">
            <div className="space-y-1.5">
              <TsLabel>Username</TsLabel>
              <div className="flex items-stretch">
                <TsInput value={rabbitMqUsername ?? ''} readOnly className="rounded-r-none" />
                <TsButton
                  type="button"
                  variant="outline"
                  size="icon"
                  className="rounded-l-none border-l-0"
                  onClick={() => copyValue('username', rabbitMqUsername)}
                  disabled={!rabbitMqUsername}
                  aria-label="Copy username"
                  title="Copy value"
                >
                  {copiedField === 'username' ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                </TsButton>
              </div>
            </div>
            <div className="space-y-1.5">
              <TsLabel>Password</TsLabel>
              <div className="flex items-stretch">
                <div className="relative flex-1">
                  <TsInput
                    type={showPassword ? 'text' : 'password'}
                    value={rabbitMqPassword ?? ''}
                    readOnly
                    className="rounded-r-none pr-10"
                  />
                  <TsButton
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="absolute right-1 top-1/2 h-7 w-7 -translate-y-1/2"
                    onClick={() => setShowPassword(prev => !prev)}
                    aria-label={showPassword ? 'Hide password' : 'Show password'}
                    title={showPassword ? 'Hide value' : 'Show value'}
                  >
                    {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                  </TsButton>
                </div>
                <TsButton
                  type="button"
                  variant="outline"
                  size="icon"
                  className="rounded-l-none border-l-0"
                  onClick={() => copyValue('password', rabbitMqPassword)}
                  disabled={!rabbitMqPassword}
                  aria-label="Copy password"
                  title="Copy value"
                >
                  {copiedField === 'password' ? <Check className="h-4 w-4" /> : <Copy className="h-4 w-4" />}
                </TsButton>
              </div>
            </div>
          </div>
        </TsCardContent>
      </TsCard>
    </div>
  )
}
