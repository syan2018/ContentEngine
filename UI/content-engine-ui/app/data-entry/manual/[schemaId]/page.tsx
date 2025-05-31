import { Button } from "@/components/ui/button"
import { ArrowLeft } from "lucide-react"
import Link from "next/link"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import ManualSingleEntryForm from "@/components/data-entry/manual-single-entry-form"
import ManualTableEntry from "@/components/data-entry/manual-table-entry"
import type { Schema } from "@/lib/types" // 假设类型定义文件

// 模拟获取 Schema 信息的函数
const getMockSchema = (schemaId: string): Schema => {
  // 在实际应用中，这里会从 API 或数据库获取 Schema
  const schemas: Record<string, Schema> = {
    "1": {
      id: "1",
      name: "游戏角色",
      key: "game_character",
      description: "定义游戏中的角色属性和特征",
      fields: [
        {
          id: "f1",
          name: "character_name",
          displayName: "角色名称",
          type: "string",
          required: true,
          description: "角色的唯一名称",
        },
        {
          id: "f2",
          name: "race",
          displayName: "种族",
          type: "enum",
          required: true,
          options: ["人类", "精灵", "矮人", "兽人"],
          description: "角色的种族",
        },
        { id: "f3", name: "class", displayName: "职业", type: "string", required: true, description: "角色的主要职业" },
        { id: "f4", name: "level", displayName: "等级", type: "number", required: true, description: "角色的当前等级" },
        {
          id: "f5",
          name: "health_points",
          displayName: "生命值",
          type: "number",
          required: true,
          description: "角色的当前生命值",
        },
        {
          id: "f6",
          name: "is_active",
          displayName: "是否活跃",
          type: "boolean",
          required: false,
          description: "角色当前是否活跃",
        },
        {
          id: "f7",
          name: "bio",
          displayName: "角色简介",
          type: "text",
          required: false,
          description: "角色的背景故事或简介",
        },
        {
          id: "f8",
          name: "inventory_items",
          displayName: "物品栏",
          type: "array",
          required: false,
          description: "角色携带的物品列表 (逗号分隔)",
        },
        {
          id: "f9",
          name: "attributes",
          displayName: "属性",
          type: "object",
          required: false,
          description: "角色的核心属性 (JSON格式)",
        },
        {
          id: "f10",
          name: "last_seen_date",
          displayName: "最后出现日期",
          type: "date",
          required: false,
          description: "角色最后一次出现的日期",
        },
      ],
    },
    // 可以添加更多 Schema 定义
  }
  return schemas[schemaId] || schemas["1"] // 默认返回第一个
}

export default function ManualDataEntryPage({ params }: { params: { schemaId: string } }) {
  const schema = getMockSchema(params.schemaId)

  if (!schema) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-2">
          <Link href="/data-entry">
            <Button variant="ghost" size="sm">
              <ArrowLeft className="mr-2 h-4 w-4" />
              返回
            </Button>
          </Link>
        </div>
        <p>未找到ID为 {params.schemaId} 的数据结构。</p>
      </div>
    )
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
        <div className="text-muted-foreground">信息注入 / 手动录入</div>
      </div>

      <div>
        <h1 className="text-3xl font-bold tracking-tight">{schema.name}</h1>
        <p className="text-muted-foreground">{schema.description}</p>
      </div>

      <Tabs defaultValue="single-entry" className="space-y-4">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="single-entry">单个录入</TabsTrigger>
          <TabsTrigger value="table-entry">表格录入</TabsTrigger>
        </TabsList>
        <TabsContent value="single-entry" className="space-y-4">
          <ManualSingleEntryForm schema={schema} />
        </TabsContent>
        <TabsContent value="table-entry" className="space-y-4">
          <ManualTableEntry schema={schema} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
