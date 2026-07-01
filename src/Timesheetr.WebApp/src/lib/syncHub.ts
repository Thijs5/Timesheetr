import { HubConnectionBuilder, LogLevel, type HubConnection } from '@microsoft/signalr'
import type { EntryStatusChanged } from '../types'

let connection: HubConnection | null = null

function getConnection() {
  connection ??= new HubConnectionBuilder()
    .withUrl('/hubs/sync-status')
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Warning)
    .build()
  return connection
}

// Wolverine.SignalR wraps every message in a light CloudEvents-style
// envelope ({ type, data }) and always invokes the "ReceiveMessage" method.
export function subscribeToEntryStatus(
  onStatusChanged: (status: EntryStatusChanged) => void,
  onReconnected: () => void
) {
  const hub = getConnection()

  hub.on('ReceiveMessage', (raw: string) => {
    try {
      const envelope = JSON.parse(raw) as { type: string; data: EntryStatusChanged }
      onStatusChanged(envelope.data)
    } catch {
      /* ignore malformed */
    }
  })

  hub.onreconnected(onReconnected)

  if (hub.state === 'Disconnected') void hub.start()

  return () => {
    hub.off('ReceiveMessage')
  }
}
