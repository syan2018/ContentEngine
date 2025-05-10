import { Button } from "@/components/ui/button"
import { ArrowLeft } from "lucide-react"
import Link from "next/link"
import AIDataEntryWizard from "@/components/data-entry/ai-data-entry-wizard"

export default function AIDataEntry({ params }: { params: { schemaId: string } }) {
  // 这里可以根据 schemaId 获取实际的 schema 信息
  const schemaInfo = {
    id: params.schemaId,
    name: "游戏角色",
    description: "定义游戏中的角色属性和特征",
    fields: [
      { id: 1, name: "name", displayName: "角色名称", type: "string", required: true },
      { id: 2, name: "race", displayName: "种族", type: "string", required: true },
      { id: 3, name: "class", displayName: "职业", type: "string", required: true },
      { id: 4, name: "level", displayName: "等级", type: "number", required: true },
      { id: 5, name: "health", displayName: "生命值", type: "number", required: true },
      { id: 6, name: "skills", displayName: "技能列表", type: "array", required: false },
      { id: 7, name: "background", displayName: "背景故事", type: "text", required: false },
    ],
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Link href="/data-entry">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            返回
          </Button>
        </Link>
        <div className="text-muted-foreground">信息注入 / AI 辅助录入</div>
      </div>

      <div>
        <h1 className="text-3xl font-bold tracking-tight">{schemaInfo.name}</h1>
        <p className="text-muted-foreground">{schemaInfo.description}</p>
      </div>

      <AIDataEntryWizard schema={schemaInfo} />
    </div>
  )
}
