import { cn } from '@/lib/utils'

interface TsStatTileProps {
  label: string
  value: string
  className?: string
}

export function TsStatTile({ label, value, className }: TsStatTileProps) {
  return (
    <div className={cn('rounded-lg border bg-card px-4 py-3', className)}>
      <div className="text-xs text-muted-foreground">{label}</div>
      <div className="mt-1 text-xl font-semibold tracking-tight text-foreground">{value}</div>
    </div>
  )
}
