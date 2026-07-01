import { TsCardHeader, TsCardTitle } from '@/components/atoms/TsCard'

function IntegrationLogo({
  icon: Icon,
  logoSrc,
  label,
  color,
}: {
  icon?: React.ComponentType<{ className?: string }>
  logoSrc?: string
  label: string
  color: string
}) {
  return (
    <div
      className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg"
      style={{ backgroundColor: `${color}1a`, color }}
    >
      {logoSrc ? (
        <img src={logoSrc} alt={`${label} logo`} className="h-5 w-5 object-contain" />
      ) : Icon ? (
        <Icon className="h-5 w-5" />
      ) : (
        <span className="text-sm font-semibold">{label[0]}</span>
      )}
    </div>
  )
}

export function IntegrationHeader({
  icon,
  logoSrc,
  color,
  title,
  description,
}: {
  icon?: React.ComponentType<{ className?: string }>
  logoSrc?: string
  color: string
  title: string
  description: string
}) {
  return (
    <TsCardHeader className="flex-row items-center gap-3 space-y-0 pb-4">
      <IntegrationLogo icon={icon} logoSrc={logoSrc} label={title} color={color} />
      <div>
        <TsCardTitle className="text-base">{title}</TsCardTitle>
        <p className="text-xs text-muted-foreground mt-0.5">{description}</p>
      </div>
    </TsCardHeader>
  )
}
