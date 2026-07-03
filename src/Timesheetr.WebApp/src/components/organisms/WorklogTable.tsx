import { useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Loader2 } from 'lucide-react'
import { TsButton } from '@/components/atoms/TsButton'
import { TsBadge } from '@/components/atoms/TsBadge'
import { TsCard } from '@/components/atoms/TsCard'
import { TsAlert } from '@/components/atoms/TsAlert'
import { TsStatTile } from '@/components/atoms/TsStatTile'
import { DateRangePicker } from '@/components/molecules/DateRangePicker'
import { DailyHoursChart, type DailyHoursDatum } from '@/components/molecules/DailyHoursChart'
import { api } from '@/lib/api'
import { formatDate, formatHM, toIsoDate, getWeekStart } from '@/lib/utils'
import type { TempoWorklog } from '@/types'

function defaultFrom() {
  return toIsoDate(getWeekStart(new Date()))
}
function defaultTo() {
  return toIsoDate(new Date())
}

export function WorklogTable() {
  const [searchParams, setSearchParams] = useSearchParams()
  const from = searchParams.get('from') ?? defaultFrom()
  const to = searchParams.get('to') ?? defaultTo()
  const queryFrom = searchParams.get('q_from')
  const queryTo = searchParams.get('q_to')

  const { data: worklogs, isLoading, error } = useQuery({
    queryKey: ['worklogs', queryFrom, queryTo],
    queryFn: () => api.getWorklogs(queryFrom!, queryTo!),
    enabled: !!queryFrom && !!queryTo,
  })

  const setRange = (f: string, t: string) => setSearchParams(prev => { prev.set('from', f); prev.set('to', t); return prev })

  const load = () => {
    setSearchParams(prev => {
      prev.set('q_from', from)
      prev.set('q_to', to)
      return prev
    })
  }

  const grouped = Object.entries(
    (worklogs ?? []).reduce<Record<string, TempoWorklog[]>>((acc, w) => {
      ;(acc[w.startDate] ??= []).push(w)
      return acc
    }, {})
  ).sort(([a], [b]) => a.localeCompare(b))

  const totalSeconds = (worklogs ?? []).reduce((s, w) => s + w.timeSpentSeconds, 0)
  const daysWorked = grouped.length
  const avgPerDaySeconds = daysWorked > 0 ? totalSeconds / daysWorked : 0

  const chartData: DailyHoursDatum[] = grouped.map(([date, dayWorklogs]) => ({
    date,
    totalSeconds: dayWorklogs.reduce((s, w) => s + w.timeSpentSeconds, 0),
  }))

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Tempo Worklogs</h1>
        <p className="text-sm text-muted-foreground mt-1">View worklogs logged to Tempo</p>
      </div>

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_18rem] lg:items-start">
        <div className="space-y-6 min-w-0">
          <div className="flex flex-wrap items-end gap-4">
            <DateRangePicker from={from} to={to} onRangeChange={setRange} />
            <TsButton onClick={load} disabled={isLoading}>
              {isLoading && <Loader2 className="h-4 w-4 animate-spin" />}
              Load from Tempo
            </TsButton>
          </div>

          {error && <TsAlert variant="destructive" message={(error as Error).message} />}

          {worklogs && worklogs.length > 0 && (
            <>
              <p className="text-sm text-muted-foreground">{worklogs.length} worklogs</p>

              <TsCard>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="w-28 px-4 py-2.5 text-left font-medium text-muted-foreground">Issue</th>
                        <th className="px-4 py-2.5 text-left font-medium text-muted-foreground">Description</th>
                        <th className="w-20 px-4 py-2.5 text-left font-medium text-muted-foreground">Duration</th>
                      </tr>
                    </thead>
                    <tbody>
                      {grouped.map(([date, dayWorklogs]) => {
                        const daySeconds = dayWorklogs.reduce((s, w) => s + w.timeSpentSeconds, 0)
                        return [
                          <tr key={`day-${date}`} className="border-b bg-muted/20">
                            <td colSpan={2} className="px-4 py-1.5 text-xs font-semibold text-muted-foreground">
                              {formatDate(date)}
                            </td>
                            <td className="px-4 py-1.5 text-xs font-semibold text-muted-foreground text-right">
                              {formatHM(daySeconds)}
                            </td>
                          </tr>,
                          ...dayWorklogs.map(w => (
                            <tr key={w.tempoWorklogId} className="border-b hover:bg-muted/30 transition-colors">
                              <td className="px-4 py-2">
                                <TsBadge variant="secondary">{w.issue.key ?? `#${w.issue.id}`}</TsBadge>
                              </td>
                              <td className="px-4 py-2 text-muted-foreground">{w.description}</td>
                              <td className="px-4 py-2 tabular-nums text-muted-foreground">{w.formattedDuration}</td>
                            </tr>
                          )),
                        ]
                      })}
                    </tbody>
                  </table>
                </div>
              </TsCard>
            </>
          )}

          {!isLoading && queryFrom && worklogs?.length === 0 && (
            <div className="text-center text-muted-foreground py-16">No worklogs found for this period.</div>
          )}

          {!isLoading && !queryFrom && (
            <div className="text-center text-muted-foreground py-16">
              Pick a date range and click <span className="font-medium text-foreground">Load from Tempo</span> to get started.
            </div>
          )}
        </div>

        {worklogs && worklogs.length > 0 && (
          <TsCard className="lg:sticky lg:top-8">
            <div className="p-5 space-y-5">
              <div className="grid grid-cols-1 gap-3">
                <TsStatTile label="Total logged" value={formatHM(totalSeconds)} />
                <TsStatTile label="Days worked" value={String(daysWorked)} />
                <TsStatTile label="Avg per day" value={formatHM(avgPerDaySeconds)} />
              </div>
              <div>
                <h3 className="mb-2 text-xs font-medium text-muted-foreground">Daily hours</h3>
                <DailyHoursChart data={chartData} orientation="rows" />
              </div>
            </div>
          </TsCard>
        )}
      </div>
    </div>
  )
}
