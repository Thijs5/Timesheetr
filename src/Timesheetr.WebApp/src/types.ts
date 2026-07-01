export type SyncStatus = 'Pending' | 'Syncing' | 'Synced' | 'Error'

export interface TimesheetEntry {
  togglId: number
  workspaceId: number
  selected: boolean
  issueKey: string
  description: string
  date: string
  startTime: string
  durationSeconds: number
  status: SyncStatus
  errorMessage?: string
  loggedToTempo: boolean
  hasValidIssueKey: boolean
  formattedDuration: string
  togglRetryAttempt: number
  nextRetryAt?: string
}

export interface TempoWorklog {
  tempoWorklogId: number
  issue: { id: number; key: string | null; display: string }
  timeSpentSeconds: number
  startDate: string
  startTime: string
  description: string
  formattedDuration: string
}

export interface AppSettings {
  togglApiToken: string
  tempoApiToken: string
  jiraAccountId: string
  jiraBaseUrl: string
  jiraEmail: string
  jiraApiToken: string
  isConfigured: boolean
}

export interface EntryStatusChanged {
  togglId: number
  status: SyncStatus
  errorMessage?: string
  loggedToTempo: boolean
  nextRetryAt?: string
}

export interface SyncEntriesResponse {
  queuedTogglIds: number[]
  skippedTogglIds: number[]
}
