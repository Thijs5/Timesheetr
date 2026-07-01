import { ExternalLink } from 'lucide-react'
import { SiRabbitmq } from 'react-icons/si'
import { TsInput } from '@/components/atoms/TsInput'
import { TsLabel } from '@/components/atoms/TsLabel'
import { TsCard, TsCardContent } from '@/components/atoms/TsCard'
import { IntegrationHeader } from '@/components/molecules/IntegrationHeader'

const rabbitMqManagementUrl = import.meta.env.VITE_RABBITMQ_MANAGEMENT_URL as string | undefined
const rabbitMqUsername = import.meta.env.VITE_RABBITMQ_USERNAME as string | undefined
const rabbitMqPassword = import.meta.env.VITE_RABBITMQ_PASSWORD as string | undefined

export function InfrastructureSettings() {
  if (!rabbitMqManagementUrl) {
    return <p className="text-sm text-muted-foreground">No infrastructure services configured.</p>
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
              <TsInput value={rabbitMqUsername ?? ''} readOnly />
            </div>
            <div className="space-y-1.5">
              <TsLabel>Password</TsLabel>
              <TsInput value={rabbitMqPassword ?? ''} readOnly />
            </div>
          </div>
        </TsCardContent>
      </TsCard>
    </div>
  )
}
