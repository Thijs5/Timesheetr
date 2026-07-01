import type { TimesheetEntry, TempoWorklog, AppSettings, SyncEntriesResponse } from '../types'

const BASE = '/api'

async function get<T>(path: string): Promise<T> {
  const res = await fetch(`${BASE}${path}`)
  if (!res.ok) {
    const err = await res.json().catch(() => ({ error: res.statusText }))
    throw new Error(err.error ?? res.statusText)
  }
  return res.json() as Promise<T>
}

export const api = {
  getEntries: (from: string, to: string) =>
    get<TimesheetEntry[]>(`/entries?from=${from}&to=${to}`),

  getWorklogs: (from: string, to: string) =>
    get<TempoWorklog[]>(`/worklogs?from=${from}&to=${to}`),

  getSettings: () => get<AppSettings>('/settings'),

  saveSettings: (settings: Omit<AppSettings, 'isConfigured'>) =>
    fetch(`${BASE}/settings`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(settings),
    }),

  syncEntries: async (entries: TimesheetEntry[]) => {
    const res = await fetch(`${BASE}/entries/sync`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ entries }),
    })
    if (!res.ok) {
      const err = await res.json().catch(() => ({ error: res.statusText }))
      throw new Error(err.error ?? res.statusText)
    }
    return res.json() as Promise<SyncEntriesResponse>
  },

  resetEntry: (togglId: number, workspaceId: number) =>
    fetch(`${BASE}/entries/${togglId}/reset`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ workspaceId }),
    }),
}
