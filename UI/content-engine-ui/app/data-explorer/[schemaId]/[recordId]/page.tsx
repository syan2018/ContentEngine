import { Button } from "@/components/ui/button"
import { ArrowLeft, Edit, Trash2 } from "lucide-react"
import Link from "next/link"
import RecordDetailView from "@/components/data-explorer/record-detail-view"

export default function RecordDetail({ params }: { params: { schemaId: string; recordId: string } }) {
  // 这里可以根据 schemaId 和 recordId 获取实际的数据
  const schemaInfo = {
    id: params.schemaId,
    name: "游戏角色",
  }

  const recordInfo = {
    id: params.recordId,
    name: "艾莉娅",
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Link href={`/data-explorer/${params.schemaId}`}>
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            返回列表
          </Button>
        </Link>
        <div className="text-muted-foreground">{schemaInfo.name} / 记录详情</div>
      </div>

      <div className="flex items-center justify-between">
        <h1 className="text-3xl font-bold tracking-tight">{recordInfo.name}</h1>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm">
            <Edit className="mr-2 h-4 w-4" />
            编辑
          </Button>
          <Button variant="destructive" size="sm">
            <Trash2 className="mr-2 h-4 w-4" />
            删除
          </Button>
        </div>
      </div>

      <RecordDetailView schemaId={params.schemaId} recordId={params.recordId} />
    </div>
  )
}
