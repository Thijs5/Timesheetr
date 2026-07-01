import { BrowserRouter, Routes, Route } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { MainLayout } from '@/components/layout/MainLayout'
import { SyncPage } from '@/pages/SyncPage'
import { TempoPage } from '@/pages/TempoPage'
import { SettingsPage } from '@/pages/SettingsPage'
import { IntegrationsPage } from '@/pages/IntegrationsPage'
import { InfrastructurePage } from '@/pages/InfrastructurePage'

const queryClient = new QueryClient({
  defaultOptions: { queries: { retry: 1, staleTime: 30_000 } },
})

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Routes>
          <Route element={<MainLayout />}>
            <Route index element={<SyncPage />} />
            <Route path="tempo" element={<TempoPage />} />
            <Route path="settings" element={<SettingsPage />}>
              <Route index element={<IntegrationsPage />} />
              <Route path="infrastructure" element={<InfrastructurePage />} />
            </Route>
          </Route>
        </Routes>
      </BrowserRouter>
    </QueryClientProvider>
  )
}
