# ADR-0002: Atomic Component Design and Anti-Corruption Layer for UI Components

## Status
Accepted

## Context
Timesheetr uses shadcn/ui as its React component library. Directly referencing shadcn/ui components throughout pages and feature slices creates tight coupling to the library: an upgrade, replacement, or customisation requires touching every consumer.

Additionally, as components grow in complexity, ad-hoc inline CSS (padding, margin, colour overrides) accumulates and erodes visual consistency.

## Decision

### Atomic folder structure
Organise custom UI components in `web/src/components/` using atomic design levels:

```
web/src/components/
  atoms/          ← smallest units: Button, Badge, Input, Icon
  molecules/      ← combinations of atoms: SearchBar, FilterRow, StatusBadge
  organisms/      ← full sections: EntryTable, SyncPanel, PageHeader
  layout/         ← MainLayout, NavMenu, Sidebar
  ui/             ← raw shadcn/ui generated components (do not use directly in pages)
```

Pages compose from organisms and molecules. Atoms and molecules are never imported directly by page components.

### Anti-corruption layer
Every shadcn/ui component used by the application is wrapped in a custom component that lives in `components/atoms/` or `components/molecules/`. Pages and organisms reference only the custom wrappers, never `ui/` components directly.

```tsx
// components/atoms/TsButton.tsx
import { Button } from "@/components/ui/button";

export function TsButton({ variant, size, children, ...props }) {
  return <Button variant={variant} size={size} {...props}>{children}</Button>;
}
```

The `Ts` prefix (Timesheetr) distinguishes custom wrappers from raw shadcn/ui components. Swapping or upgrading shadcn/ui then requires changes only in the wrapper components.

### No custom spacing or CSS in organisms and above
Organisms, layout components, and pages must not contain custom `style=` attributes, margin/padding utility classes (e.g. `mb-4`, `px-3`), or component-scoped CSS that controls spacing. All spacing is the responsibility of the atom or molecule rendering the element. This keeps layout predictable and prevents local overrides from compounding.

## Consequences
- Adding a new shadcn/ui component requires writing a wrapper first — a small upfront cost that pays off at replacement time.
- Pages and organisms are simpler and more readable: no layout noise, only structure and data flow.
- Consistent spacing across the application because spacing is defined once, at the atom/molecule level.
- Components from `components/ui/` should not appear in `pages/` or `components/organisms/` — this is enforceable via code review or lint rules.
