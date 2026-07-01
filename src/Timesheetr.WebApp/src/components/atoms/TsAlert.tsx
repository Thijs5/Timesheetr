import { Alert, AlertDescription } from '@/components/ui/alert'

interface TsAlertProps extends React.HTMLAttributes<HTMLDivElement> {
  variant?: 'default' | 'destructive' | 'warning'
  message: React.ReactNode
}

export function TsAlert({ message, variant, ...props }: TsAlertProps) {
  return (
    <Alert variant={variant} {...props}>
      <AlertDescription>{message}</AlertDescription>
    </Alert>
  )
}
