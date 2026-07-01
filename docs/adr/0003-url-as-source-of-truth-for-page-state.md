# ADR-0003: URL as Source of Truth for Page State

## Status
Accepted

## Context
Timesheetr pages carry state that is meaningful to the user: date range filters, selected entries, active tabs. This state can be stored in several places: component state (`useState`), a client-side state store, or the URL.

Storing state in component state loses it on navigation or refresh. A client-side store survives navigation within a session but not a page reload or a shared link. Neither approach lets a user bookmark a filtered view, copy a URL to a colleague, or press F5 and land back in the same place.

## Decision
The URL is the single source of truth for all page-level state. This covers:

- Filter inputs (date range, text, enum)
- Selected tab or active section
- Sort column and direction
- Pagination offset / page number
- Any control that narrows or changes what is displayed

**Mechanics in React**

State is read from the URL using React Router's `useSearchParams()` hook. Controls that mutate state call `setSearchParams()`, which updates the URL without a full page reload. Navigation components (tabs, pagination links) render as `<Link>` elements with the full target URL already encoded.

```tsx
const [searchParams, setSearchParams] = useSearchParams();
const from = searchParams.get("from") ?? defaultFrom;

function handleDateChange(value: string) {
  setSearchParams(prev => { prev.set("from", value); return prev; });
}
```

Component state holds derived or transient data only (loaded API results, loading flags, error messages). It is never the primary representation of what the user has chosen.

**What is excluded**

Ephemeral UI state with no navigational meaning is exempt: hover state, focus, open/closed state of a tooltip or confirmation dialog. These do not survive a refresh by design.

## Consequences
- Reloading a page always restores the user's exact view.
- Filtered or configured views can be bookmarked and shared by copying the URL.
- Browser back/forward navigation works correctly across filter and date range changes.
- Component code is simpler: there is no state to initialise, persist, or synchronise — `useSearchParams` covers all of it.
- Deep-linking works out of the box with no additional infrastructure.
