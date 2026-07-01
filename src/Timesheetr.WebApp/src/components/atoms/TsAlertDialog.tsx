import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
} from '@/components/ui/alert-dialog'

interface TsAlertDialogProps {
  trigger: React.ReactNode
  title: string
  description: React.ReactNode
  confirmLabel: string
  confirmVariant?: 'default' | 'destructive'
  onConfirm: () => void
}

export function TsAlertDialog({
  trigger,
  title,
  description,
  confirmLabel,
  confirmVariant = 'default',
  onConfirm,
}: TsAlertDialogProps) {
  const destructiveClass = confirmVariant === 'destructive'
    ? 'bg-destructive text-destructive-foreground hover:bg-destructive/90'
    : ''

  return (
    <AlertDialog>
      <AlertDialogTrigger asChild>{trigger}</AlertDialogTrigger>
      <AlertDialogContent
        onEscapeKeyDown={(e: KeyboardEvent) => e.preventDefault()}
      >
        <AlertDialogHeader>
          <AlertDialogTitle>{title}</AlertDialogTitle>
          <AlertDialogDescription>{description}</AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel>Cancel</AlertDialogCancel>
          <AlertDialogAction className={destructiveClass} onClick={onConfirm}>
            {confirmLabel}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
