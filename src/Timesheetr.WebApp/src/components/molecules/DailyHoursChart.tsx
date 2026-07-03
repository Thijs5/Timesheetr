import { useState } from 'react'
import { formatDate, formatHM } from '@/lib/utils'

export interface DailyHoursDatum {
  date: string
  totalSeconds: number
  syncedSeconds?: number
}

interface DailyHoursChartProps {
  data: DailyHoursDatum[]
  /** 'columns' for a wide layout, 'rows' for a narrow vertical sidebar. */
  orientation?: 'columns' | 'rows'
}

// Single-hue sequential blue: darker step = synced (filled), lighter step = pending (unfilled track).
const SYNCED_COLOR = '#184f95'
const PENDING_COLOR = '#9ec5f4'

export function DailyHoursChart({ data, orientation = 'columns' }: DailyHoursChartProps) {
  const [hovered, setHovered] = useState<string | null>(null)
  const maxSeconds = Math.max(...data.map(d => d.totalSeconds), 1)
  const hasSyncedBreakdown = data.some(d => d.syncedSeconds !== undefined)

  if (data.length === 0) return null

  const legend = hasSyncedBreakdown && (
    <div className="flex items-center gap-4 text-xs text-muted-foreground">
      <span className="inline-flex items-center gap-1.5">
        <span className="h-2 w-2 rounded-full" style={{ backgroundColor: SYNCED_COLOR }} />
        Synced
      </span>
      <span className="inline-flex items-center gap-1.5">
        <span className="h-2 w-2 rounded-full" style={{ backgroundColor: PENDING_COLOR }} />
        Pending
      </span>
    </div>
  )

  if (orientation === 'rows') {
    return (
      <div className="space-y-2">
        {data.map(d => {
          const syncedSeconds = d.syncedSeconds ?? d.totalSeconds
          const pendingSeconds = d.totalSeconds - syncedSeconds
          const syncedPct = (syncedSeconds / maxSeconds) * 100
          const pendingPct = (pendingSeconds / maxSeconds) * 100
          return (
            <div key={d.date} className="flex items-center gap-2">
              <div className="w-9 shrink-0 text-[11px] text-muted-foreground">{shortDay(d.date)}</div>
              <div className="relative h-2 flex-1 rounded-full bg-muted">
                <div
                  className="absolute inset-y-0 left-0 rounded-full"
                  style={{ width: `${syncedPct}%`, backgroundColor: SYNCED_COLOR }}
                />
                <div
                  className="absolute inset-y-0 rounded-full"
                  style={{ left: `${syncedPct}%`, width: `${pendingPct}%`, backgroundColor: PENDING_COLOR }}
                />
              </div>
              <div className="w-16 shrink-0 whitespace-nowrap text-right text-[11px] tabular-nums text-muted-foreground">
                {formatHM(d.totalSeconds)}
              </div>
            </div>
          )
        })}
        {legend}
      </div>
    )
  }

  return (
    <div>
      <div className="flex items-end gap-2 h-32 overflow-x-auto px-1 pt-6">
        {data.map(d => {
          const syncedSeconds = d.syncedSeconds ?? d.totalSeconds
          const pendingSeconds = d.totalSeconds - syncedSeconds
          const syncedPct = (syncedSeconds / maxSeconds) * 100
          const pendingPct = (pendingSeconds / maxSeconds) * 100
          const isHovered = hovered === d.date

          return (
            <div
              key={d.date}
              className="relative flex h-full min-w-[28px] flex-1 flex-col items-center justify-end"
              onMouseEnter={() => setHovered(d.date)}
              onMouseLeave={() => setHovered(null)}
            >
              {isHovered && (
                <div className="absolute bottom-full z-10 mb-1.5 whitespace-nowrap rounded-md border bg-popover px-2.5 py-1.5 text-xs shadow-md">
                  <div className="font-medium text-popover-foreground">{formatDate(d.date)}</div>
                  <div className="text-muted-foreground">{formatHM(d.totalSeconds)} tracked</div>
                  {d.syncedSeconds !== undefined && (
                    <div className="text-muted-foreground">{formatHM(d.syncedSeconds)} synced</div>
                  )}
                </div>
              )}
              <div className="relative w-full max-w-6" style={{ height: '100%', opacity: hovered && !isHovered ? 0.55 : 1 }}>
                {pendingPct > 0 && (
                  <div
                    className="absolute inset-x-0"
                    style={{
                      bottom: `${syncedPct}%`,
                      height: `${pendingPct}%`,
                      backgroundColor: PENDING_COLOR,
                      borderRadius: '4px 4px 0 0',
                    }}
                  />
                )}
                <div
                  className="absolute inset-x-0 bottom-0"
                  style={{
                    height: `${syncedPct}%`,
                    backgroundColor: SYNCED_COLOR,
                    borderRadius: pendingPct > 0 ? 0 : '4px 4px 0 0',
                  }}
                />
              </div>
              <div className="mt-1.5 text-[10px] text-muted-foreground">{shortDay(d.date)}</div>
            </div>
          )
        })}
      </div>
      {legend && <div className="mt-2">{legend}</div>}
    </div>
  )
}

function shortDay(dateStr: string): string {
  const date = new Date(dateStr + 'T00:00:00')
  return date.toLocaleDateString('en-GB', { weekday: 'short' })
}
