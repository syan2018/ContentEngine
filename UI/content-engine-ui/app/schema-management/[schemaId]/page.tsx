import { Button } from "@/components/ui/button"
import { ArrowLeft } from "lucide-react"
import Link from "next/link"
import SchemaDetailView from "@/components/schema/schema-detail-view"

export default function SchemaDetailPage({ params }: { params: { schemaId: string } }) {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Link href="/schema-management">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            返回Schema列表
          </Button>
        </Link>
        <div className="text-muted-foreground">数据管理 / Schema详情</div>
      </div>

      <SchemaDetailView schemaId={params.schemaId} />
    </div>
  )
}
