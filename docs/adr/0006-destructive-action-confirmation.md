# ADR-0006: Destructive Action Confirmation

## Status
Accepted

## Context
Timesheetr can trigger operations that modify state in external systems (Toggl, Tempo). Users may accidentally trigger destructive or impactful actions (e.g., resetting a synced entry, re-syncing with overwrite) through misclicks or misunderstanding the consequences. Once executed, some of these actions may be difficult or impossible to reverse.

## Decision
Every destructive action in the application must display a confirmation dialog before execution. A "destructive action" is any operation that:
- Modifies or deletes data in an external system (Toggl, Tempo, Jira)
- Removes a record from the local sync log
- Cannot be easily undone

### Implementation
Use an `AlertDialog` component for confirmations. The dialog must:
- **Clearly explain the specific action** — The description must state exactly what will happen, including relevant context (e.g., which entry, what side effects). The user should understand the full impact without needing prior knowledge.
- Require explicit user confirmation (clicking a confirm button)
- Prevent dismissal via click-outside or Escape key (user must make an active choice)
- Use the `destructive` button variant for the confirm action when the operation is irreversible

### Dialog content guidelines
- **Title**: Short imperative phrase (e.g., "Reset Entry", "Re-sync All")
- **Description**: Must answer "What exactly will happen?" — include:
  - The specific item or entity affected (description, date, issue key)
  - Any external side effects (e.g., "This will remove the Toggl sync tag", "The Tempo worklog will not be deleted")
  - Whether the action can be undone
- **Action button**: Label should match the action (e.g., "Reset", "Re-sync") — avoid generic "OK" or "Confirm"

### Examples of actions requiring confirmation
- Reset a synced entry (removes sync record, removes Toggl tag)
- Re-sync entries that have already been logged to Tempo
- Clear the entire sync history

### Examples of actions NOT requiring confirmation
- Selecting or deselecting entries for a new sync
- Changing the date range filter
- Expanding/collapsing sections
- Read-only operations (viewing logs, refreshing data)

## Consequences
- Users are protected from accidental destructive operations
- Each destructive action adds one extra click, which is an acceptable trade-off for safety
- Developers must evaluate whether new actions are destructive and add confirmation accordingly
- Consistent UX pattern across the application — users learn to expect confirmation for impactful actions
