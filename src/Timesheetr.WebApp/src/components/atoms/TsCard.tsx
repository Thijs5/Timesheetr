import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'

export function TsCard(props: React.HTMLAttributes<HTMLDivElement>) {
  return <Card {...props} />
}

export function TsCardContent({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <CardContent className={className} {...props} />
}

export function TsCardHeader({ className, ...props }: React.HTMLAttributes<HTMLDivElement>) {
  return <CardHeader className={className} {...props} />
}

export function TsCardTitle({ className, ...props }: React.HTMLAttributes<HTMLHeadingElement>) {
  return <CardTitle className={className} {...props} />
}
