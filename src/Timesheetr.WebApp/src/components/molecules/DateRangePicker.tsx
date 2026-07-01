import { useState } from 'react'
import { format } from 'date-fns'
import { CalendarIcon } from 'lucide-react'
import type { DateRange } from 'react-day-picker'
import { TsButton } from '@/components/atoms/TsButton'
import { Calendar } from '@/components/ui/calendar'
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover'
import { toIsoDate, getWeekStart } from '@/lib/utils'

interface DateRangePickerProps {
  from: string
  to: string
  onRangeChange: (from: string, to: string) => void
}

type Preset = 'thisWeek' | 'lastWeek' | 'thisMonth' | 'lastMonth'

const presetLabels: Record<Preset, string> = {
  thisWeek: 'This week',
  lastWeek: 'Last week',
  thisMonth: 'This month',
  lastMonth: 'Last month',
}

function parseIsoDate(value: string) {
  return new Date(value + 'T00:00:00')
}

export function DateRangePicker({ from, to, onRangeChange }: DateRangePickerProps) {
  const [open, setOpen] = useState(false)
  const [draft, setDraft] = useState<DateRange | undefined>(undefined)

  const committedRange: DateRange = { from: parseIsoDate(from), to: parseIsoDate(to) }
  const selected = draft ?? committedRange

  const handleOpenChange = (next: boolean) => {
    setOpen(next)
    setDraft(next ? committedRange : undefined)
  }

  const handleSelect = (range: DateRange | undefined) => {
    setDraft(range)
    if (range?.from && range?.to) {
      onRangeChange(toIsoDate(range.from), toIsoDate(range.to))
      setOpen(false)
    }
  }

  const applyPreset = (preset: Preset) => {
    const today = new Date()
    let range: DateRange
    if (preset === 'thisWeek') {
      range = { from: getWeekStart(today), to: today }
    } else if (preset === 'lastWeek') {
      const thisMonday = getWeekStart(today)
      const lastMonday = new Date(thisMonday)
      lastMonday.setDate(lastMonday.getDate() - 7)
      const lastSunday = new Date(thisMonday)
      lastSunday.setDate(lastSunday.getDate() - 1)
      range = { from: lastMonday, to: lastSunday }
    } else if (preset === 'thisMonth') {
      const firstDay = new Date(today.getFullYear(), today.getMonth(), 1)
      const lastDay = new Date(today.getFullYear(), today.getMonth() + 1, 0)
      range = { from: firstDay, to: lastDay }
    } else {
      const firstDay = new Date(today.getFullYear(), today.getMonth() - 1, 1)
      const lastDay = new Date(today.getFullYear(), today.getMonth(), 0)
      range = { from: firstDay, to: lastDay }
    }
    setDraft(range)
    onRangeChange(toIsoDate(range.from!), toIsoDate(range.to!))
    setOpen(false)
  }

  const label = `${format(committedRange.from!, 'MMM d, yyyy')} – ${format(committedRange.to!, 'MMM d, yyyy')}`

  return (
    <div className="space-y-1.5">
      <Popover open={open} onOpenChange={handleOpenChange}>
        <PopoverTrigger asChild>
          <TsButton variant="outline" className="w-64 justify-start font-normal">
            <CalendarIcon className="h-4 w-4" />
            {label}
          </TsButton>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <div className="flex flex-col sm:flex-row">
            <div className="flex flex-col gap-1 bg-muted p-3 sm:border-r">
              {(Object.keys(presetLabels) as Preset[]).map(p => (
                <TsButton
                  key={p}
                  variant="ghost"
                  size="sm"
                  className="justify-start"
                  onClick={() => applyPreset(p)}
                >
                  {presetLabels[p]}
                </TsButton>
              ))}
            </div>
            <Calendar
              mode="range"
              weekStartsOn={1}
              numberOfMonths={2}
              defaultMonth={committedRange.from}
              selected={selected}
              onSelect={handleSelect}
            />
          </div>
        </PopoverContent>
      </Popover>
    </div>
  )
}
