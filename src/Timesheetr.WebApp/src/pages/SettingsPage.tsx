import { NavLink, Outlet } from 'react-router-dom'
import { cn } from '@/lib/utils'

const tabs = [
  { to: '/settings', label: 'Integrations', exact: true },
  { to: '/settings/infrastructure', label: 'Infrastructure', exact: false },
]

export function SettingsPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold tracking-tight">Settings</h1>
        <p className="text-sm text-muted-foreground mt-1">Configure your integrations and API tokens</p>
      </div>

      <div className="border-b flex gap-4">
        {tabs.map(({ to, label, exact }) => (
          <NavLink
            key={to}
            to={to}
            end={exact}
            className={({ isActive }) =>
              cn(
                'pb-2 text-sm font-medium border-b-2 -mb-px transition-colors',
                isActive
                  ? 'border-primary text-foreground'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              )
            }
          >
            {label}
          </NavLink>
        ))}
      </div>

      <Outlet />
    </div>
  )
}
