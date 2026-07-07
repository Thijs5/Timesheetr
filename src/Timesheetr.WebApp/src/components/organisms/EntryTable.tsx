import { useState, useCallback, useEffect, useRef } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { Loader2, RotateCcw, AlertTriangle, ChevronDown, ChevronRight, UploadCloud } from 'lucide-react'
import { TsButton } from '@/components/atoms/TsButton'
import { TsBadge } from '@/components/atoms/TsBadge'
import { TsCard } from '@/components/atoms/TsCard'
import { TsCheckbox } from '@/components/atoms/TsCheckbox'
import { TsLabel } from '@/components/atoms/TsLabel'
import { TsAlert } from '@/components/atoms/TsAlert'
import { TsAlertDialog } from '@/components/atoms/TsAlertDialog'
import { TsStatTile } from '@/components/atoms/TsStatTile'
import { DateRangePicker } from '@/components/molecules/DateRangePicker'
import { DailyHoursChart, type DailyHoursDatum } from '@/components/molecules/DailyHoursChart'
import { api } from '@/lib/api'
import { subscribeToEntryStatus } from '@/lib/syncHub'
import { cn, formatDate, formatHM, toIsoDate, getWeekStart } from '@/lib/utils'
import type { TimesheetEntry, EntryStatusChanged } from '@/types'

function defaultFrom() {
  return toIsoDate(getWeekStart(new Date()))
}
function defaultTo() {
  return toIsoDate(new Date())
}

function formatTimeRange(startTime: string, durationSeconds: number): string {
  const [hStr, mStr, sStr = '0'] = startTime.split(':')
  const h = Number(hStr)
  const m = Number(mStr)
  const s = Number(sStr)

  if ([h, m, s].some(Number.isNaN)) {
    return `${startTime} - ?`
  }

  const startTotalSeconds = h * 3600 + m * 60 + s
  const endTotalSeconds = (startTotalSeconds + durationSeconds) % (24 * 3600)

  const toHHMM = (totalSeconds: number) => {
    const hours = Math.floor(totalSeconds / 3600)
    const minutes = Math.floor((totalSeconds % 3600) / 60)
    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}`
  }

  return `${toHHMM(startTotalSeconds)} - ${toHHMM(endTotalSeconds)}`
}

export function EntryTable() {
  const [searchParams, setSearchParams] = useSearchParams()
  const from = searchParams.get('from') ?? defaultFrom()
  const to = searchParams.get('to') ?? defaultTo()
  const showSynced = searchParams.get('showSynced') !== 'false'

  const [entries, setEntries] = useState<TimesheetEntry[]>([])
  const [hasLoaded, setHasLoaded] = useState(false)
  const [loading, setLoading] = useState(false)
  const [syncing, setSyncing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [expandedDays, setExpandedDays] = useState<Set<string>>(new Set())

  const { data: settings } = useQuery({ queryKey: ['settings'], queryFn: api.getSettings })

  const setRange = (f: string, t: string) => setSearchParams(prev => { prev.set('from', f); prev.set('to', t); return prev })
  const setShowSynced = (value: boolean) => setSearchParams(prev => { prev.set('showSynced', String(value)); return prev })

  const toggleDay = (date: string) =>
    setExpandedDays(prev => {
      const next = new Set(prev)
      next.has(date) ? next.delete(date) : next.add(date)
      return next
    })

  const loadEntries = useCallback(async () => {
    setLoading(true)
    setError(null)
    setHasLoaded(false)
    setEntries([])
    try {
      const data = await api.getEntries(from, to)
      setEntries(data)
      setHasLoaded(true)
    } catch (e) {
      setError(`Failed to load Toggl entries: ${(e as Error).message}`)
    } finally {
      setLoading(false)
    }
  }, [from, to])

  const loadEntriesRef = useRef(loadEntries)
  loadEntriesRef.current = loadEntries

  const updateEntry = (togglId: number, patch: Partial<TimesheetEntry>) =>
    setEntries(prev => prev.map(e => (e.togglId === togglId ? { ...e, ...patch } : e)))

  const handleStatusChanged = useCallback((status: EntryStatusChanged) => {
    setEntries(prev => prev.map(e => (e.togglId === status.togglId
      ? {
          ...e,
          status: status.status,
          errorMessage: status.errorMessage,
          loggedToTempo: status.loggedToTempo,
          nextRetryAt: status.nextRetryAt,
          selected: status.status === 'Synced' ? false : e.selected,
        }
      : e)))
  }, [])

  // Refetch on reconnect since a scheduled retry can resolve while disconnected,
  // and the hub only pushes to clients that are currently connected.
  useEffect(() => {
    const unsubscribe = subscribeToEntryStatus(handleStatusChanged, () => loadEntriesRef.current())
    return unsubscribe
  }, [handleStatusChanged])

  const syncToTempo = async () => {
    setSyncing(true)
    setError(null)
    const selected = entries.filter(e => e.selected && e.status !== 'Synced')
    try {
      const { queuedTogglIds, skippedTogglIds } = await api.syncEntries(selected)
      setEntries(prev => prev.map(e =>
        queuedTogglIds.includes(e.togglId) ? { ...e, status: 'Syncing' } : e
      ))
      if (skippedTogglIds.length > 0) {
        setError(
          `${skippedTogglIds.length} ${skippedTogglIds.length === 1 ? 'entry' : 'entries'} skipped — missing or invalid issue key (expected format: ABC-123).`
        )
      }
    } catch (e) {
      setError(`Sync failed: ${(e as Error).message}`)
    } finally {
      setSyncing(false)
    }
  }

  const syncSingleEntry = async (entry: TimesheetEntry) => {
    setError(null)
    try {
      const { queuedTogglIds, skippedTogglIds } = await api.syncEntries([entry])
      if (queuedTogglIds.includes(entry.togglId)) {
        updateEntry(entry.togglId, { status: 'Syncing' })
      }
      if (skippedTogglIds.length > 0) {
        setError('Entry skipped — missing or invalid issue key (expected format: ABC-123).')
      }
    } catch (e) {
      setError(`Sync failed: ${(e as Error).message}`)
    }
  }

  const resetEntry = async (entry: TimesheetEntry) => {
    try {
      const res = await api.resetEntry(entry.togglId, entry.workspaceId)
      if (!res.ok) throw new Error(await res.text())
      updateEntry(entry.togglId, { status: 'Pending', selected: true, loggedToTempo: false, errorMessage: undefined })
    } catch (e) {
      updateEntry(entry.togglId, { errorMessage: (e as Error).message })
    }
  }

  const visible = showSynced ? entries : entries.filter(e => e.status !== 'Synced')
  const pendingVisible = visible.filter(e => e.status !== 'Synced')
  const allPendingSelected = pendingVisible.length > 0 && pendingVisible.every(e => e.selected)
  const selectedCount = visible.filter(e => e.selected && e.status !== 'Synced').length
  const selectedSeconds = visible.filter(e => e.selected).reduce((s, e) => s + e.durationSeconds, 0)
  const syncedCount = entries.filter(e => e.status === 'Synced').length
  const isConfigured = settings?.isConfigured ?? true

  const totalSeconds = visible.reduce((s, e) => s + e.durationSeconds, 0)
  const syncedSeconds = visible.filter(e => e.status === 'Synced').reduce((s, e) => s + e.durationSeconds, 0)
  const daysWorked = new Set(visible.map(e => e.date)).size
  const avgPerDaySeconds = daysWorked > 0 ? totalSeconds / daysWorked : 0

  const chartData: DailyHoursDatum[] = Object.entries(
    visible.reduce<Record<string, { total: number; synced: number }>>((acc, e) => {
      const bucket = (acc[e.date] ??= { total: 0, synced: 0 })
      bucket.total += e.durationSeconds
      if (e.status === 'Synced') bucket.synced += e.durationSeconds
      return acc
    }, {})
  )
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([date, { total, synced }]) => ({ date, totalSeconds: total, syncedSeconds: synced }))

  const grouped = Object.entries(
    visible.reduce<Record<string, TimesheetEntry[]>>((acc, e) => {
      ;(acc[e.date] ??= []).push(e)
      return acc
    }, {})
  ).sort(([a], [b]) => a.localeCompare(b))

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">Timesheet Sync</h1>
          <p className="text-sm text-muted-foreground mt-1">Load your Toggl entries and sync them to Tempo</p>
        </div>
      </div>

      {!isConfigured && (
        <TsAlert
          variant="warning"
          message={
            <>
              <AlertTriangle className="h-4 w-4 inline mr-2" />
              Setup required. Please configure your API tokens on the{' '}
              <a href="/settings" className="font-medium underline underline-offset-4">Settings</a> page.
            </>
          }
        />
      )}

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-[minmax(0,1fr)_18rem] lg:items-start">
        <div className="space-y-6 min-w-0">
          <TsCard>
            <div className="p-6">
              <div className="flex flex-wrap items-end gap-4">
                <DateRangePicker from={from} to={to} onRangeChange={setRange} />
                <div className="ml-auto flex items-center gap-4">
                  <div className="flex items-center gap-2">
                    <TsCheckbox
                      id="showSynced"
                      checked={showSynced}
                      onCheckedChange={v => setShowSynced(v === true)}
                    />
                    <TsLabel htmlFor="showSynced" className="text-muted-foreground font-normal">Show synced</TsLabel>
                  </div>
                  <TsButton onClick={loadEntries} disabled={loading || !isConfigured}>
                    {loading && <Loader2 className="h-4 w-4 animate-spin" />}
                    Load from Toggl
                  </TsButton>
                </div>
              </div>
            </div>
          </TsCard>

          {error && <TsAlert variant="destructive" message={error} />}

          {visible.length > 0 && (
            <>
              <div className="flex items-center justify-between">
                <span className="text-sm text-muted-foreground">
                  {selectedCount} of {visible.length} entries selected &bull;{' '}
                  {(selectedSeconds / 3600).toFixed(1)}h total
                  {syncedCount > 0 && (
                    <TsBadge variant="success" className="ml-2">{syncedCount} synced</TsBadge>
                  )}
                </span>
                <div className="flex items-center gap-2">
                  <TsButton variant="outline" size="sm"
                    onClick={() => pendingVisible.forEach(e => updateEntry(e.togglId, { selected: true }))}>
                    Select all
                  </TsButton>
                  <TsButton variant="outline" size="sm"
                    onClick={() => pendingVisible.forEach(e => updateEntry(e.togglId, { selected: false }))}>
                    Deselect all
                  </TsButton>
                  <TsButton onClick={syncToTempo} disabled={syncing || selectedCount === 0}>
                    {syncing && <Loader2 className="h-4 w-4 animate-spin" />}
                    Sync {selectedCount} {selectedCount === 1 ? 'entry' : 'entries'} to Tempo
                  </TsButton>
                </div>
              </div>

              <TsCard>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b bg-muted/50">
                        <th className="w-10 px-3 py-2.5 text-left">
                          <TsCheckbox
                            checked={allPendingSelected}
                            onCheckedChange={v => pendingVisible.forEach(e => updateEntry(e.togglId, { selected: v === true }))}
                          />
                        </th>
                        <th className="w-28 px-3 py-2.5 text-left font-medium text-muted-foreground">Issue</th>
                        <th className="px-3 py-2.5 text-left font-medium text-muted-foreground">Description</th>
                        <th className="w-28 px-3 py-2.5 text-right font-medium text-muted-foreground">Time</th>
                        <th className="w-20 px-3 py-2.5 text-right font-medium text-muted-foreground">Duration</th>
                        <th className="w-28 px-3 py-2.5 text-center font-medium text-muted-foreground">Status</th>
                      </tr>
                    </thead>
                    <tbody>
                      {grouped.map(([date, dayEntries]) => {
                        const daySeconds = dayEntries.reduce((s, e) => s + e.durationSeconds, 0)
                        const dayLabel = formatHM(daySeconds)
                        const allSynced = dayEntries.every(e => e.status === 'Synced')
                        const dayPendingEntries = dayEntries.filter(e => e.status !== 'Synced')
                        const daySelectedCount = dayPendingEntries.filter(e => e.selected).length
                        const dayChecked: boolean | 'indeterminate' =
                          dayPendingEntries.length === 0
                            ? false
                            : daySelectedCount === 0
                              ? false
                              : daySelectedCount === dayPendingEntries.length
                                ? true
                                : 'indeterminate'
                        const isCollapsed = allSynced && !expandedDays.has(date)
                        return [
                          <tr
                            key={`day-${date}`}
                            className="border-b bg-muted/20 cursor-pointer select-none"
                            onClick={() => toggleDay(date)}
                          >
                            <td className="px-3 py-1.5" onClick={e => e.stopPropagation()}>
                              <TsCheckbox
                                checked={dayChecked}
                                disabled={dayPendingEntries.length === 0}
                                onCheckedChange={v =>
                                  dayPendingEntries.forEach(entry =>
                                    updateEntry(entry.togglId, { selected: v === true })
                                  )
                                }
                              />
                            </td>
                            <td colSpan={3} className="px-3 py-1.5 text-xs font-semibold text-muted-foreground">
                              <span className="inline-flex items-center gap-1">
                                {isCollapsed ? <ChevronRight className="h-3.5 w-3.5" /> : <ChevronDown className="h-3.5 w-3.5" />}
                                {formatDate(date)}
                              </span>
                            </td>
                            <td className="px-3 py-1.5 text-xs font-semibold text-muted-foreground tabular-nums text-right">
                              {dayLabel}
                            </td>
                            <td className="px-3 py-1.5 text-xs font-semibold text-muted-foreground text-center">
                              {allSynced && (
                                <TsBadge variant="success" className="inline-block w-20 text-center">
                                  {dayEntries.length} synced
                                </TsBadge>
                              )}
                            </td>
                          </tr>,
                          ...(isCollapsed
                            ? []
                            : dayEntries
                                .sort((a, b) => a.startTime.localeCompare(b.startTime))
                                .map(entry => (
                                  <EntryRow
                                    key={entry.togglId}
                                    entry={entry}
                                    onUpdate={patch => updateEntry(entry.togglId, patch)}
                                    onReset={() => resetEntry(entry)}
                                    onSync={() => syncSingleEntry(entry)}
                                  />
                                ))),
                        ]
                      })}
                    </tbody>
                  </table>
                </div>
              </TsCard>
            </>
          )}

          {!loading && hasLoaded && visible.length === 0 && (
            <div className="text-center text-muted-foreground py-16">No time entries found for this period.</div>
          )}

          {!loading && !hasLoaded && isConfigured && (
            <div className="text-center text-muted-foreground py-16">
              Pick a date range and click <span className="font-medium text-foreground">Load from Toggl</span> to get started.
            </div>
          )}
        </div>

        {visible.length > 0 && (
          <TsCard className="lg:sticky lg:top-8">
            <div className="p-5 space-y-5">
              <div className="grid grid-cols-2 gap-3">
                <TsStatTile label="Tracked" value={formatHM(totalSeconds)} />
                <TsStatTile label="Synced" value={formatHM(syncedSeconds)} />
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

interface EntryRowProps {
  entry: TimesheetEntry
  onUpdate: (patch: Partial<TimesheetEntry>) => void
  onReset: () => void
  onSync: () => void
}

function EntryRow({ entry, onUpdate, onReset, onSync }: EntryRowProps) {
  const isSynced = entry.status === 'Synced'
  const issueText = entry.issueKey || 'No issue key'
  const descriptionText = entry.description || 'No description'
  return (
    <tr className={cn('border-b transition-colors', !entry.selected || isSynced ? 'opacity-60' : 'hover:bg-muted/30')}>
      <td className="px-3 py-2">
        <TsCheckbox
          checked={entry.selected}
          disabled={isSynced}
          onCheckedChange={v => onUpdate({ selected: v === true })}
        />
      </td>
      <td className="px-3 py-2">
        <TsBadge
          variant="secondary"
          className={cn(!entry.hasValidIssueKey && entry.issueKey ? 'text-destructive' : '')}
        >
          {issueText}
        </TsBadge>
      </td>
      <td className="px-3 py-2">
        <span className="text-muted-foreground">{descriptionText}</span>
      </td>
      <td className="px-3 py-2 tabular-nums text-muted-foreground text-right">
        {formatTimeRange(entry.startTime, entry.durationSeconds)}
      </td>
      <td className="px-3 py-2 tabular-nums text-muted-foreground text-right">{entry.formattedDuration}</td>
      <td className="px-3 py-2">
        <EntryStatus entry={entry} onReset={onReset} onSync={onSync} />
      </td>
    </tr>
  )
}

function EntryStatus({ entry, onReset, onSync }: { entry: TimesheetEntry; onReset: () => void; onSync: () => void }) {
  switch (entry.status) {
    case 'Pending':
      return (
        <div className="flex items-center gap-1.5">
          <TsBadge variant="outline" className="text-muted-foreground">Pending</TsBadge>
          <button
            title="Sync entry"
            onClick={onSync}
            className="text-muted-foreground hover:text-foreground transition-colors"
          >
            <UploadCloud className="h-3.5 w-3.5" />
          </button>
        </div>
      )
    case 'Syncing':
      return (
        <div className="flex items-center gap-1.5">
          <Loader2 className="h-4 w-4 animate-spin text-primary" />
          {entry.nextRetryAt && (
            <span className="text-xs text-muted-foreground">
              retrying at {new Date(entry.nextRetryAt).toLocaleTimeString()}
            </span>
          )}
        </div>
      )
    case 'Synced':
      return (
        <div className="flex items-center gap-1.5">
          <TsBadge variant="success">Synced</TsBadge>
          <TsAlertDialog
            trigger={
              <button title="Reset entry" className="text-muted-foreground hover:text-foreground transition-colors">
                <RotateCcw className="h-3.5 w-3.5" />
              </button>
            }
            title="Reset Entry"
            description={
              <>
                This will remove the Toggl sync tag and the local sync record for{' '}
                <strong>{entry.description || entry.issueKey}</strong> ({entry.date}).
                The Tempo worklog will not be deleted. This cannot be undone.
              </>
            }
            confirmLabel="Reset"
            confirmVariant="destructive"
            onConfirm={onReset}
          />
        </div>
      )
    case 'Error':
      return (
        <div>
          <TsBadge variant="destructive">Error</TsBadge>
          {entry.errorMessage && (
            <p className="text-xs text-destructive mt-1 max-w-52 break-words">{entry.errorMessage}</p>
          )}
        </div>
      )
  }
}
