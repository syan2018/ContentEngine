import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import Link from "next/link"

export default function SchemaSelectionList() {
  const schemas = [
    {
      id: 1,
      name: "游戏角色",
      description: "定义游戏中的角色属性和特征",
      fieldCount: 12,
      recordCount: 48,
    },
    {
      id: 2,
      name: "游戏道具",
      description: "定义游戏中的各类道具及其属性",
      fieldCount: 8,
      recordCount: 156,
    },
    {
      id: 3,
      name: "游戏任务",
      description: "定义游戏中的任务结构和奖励",
      fieldCount: 10,
      recordCount: 32,
    },
    {
      id: 4,
      name: "游戏场景",
      description: "定义游戏中的场景和环境",
      fieldCount: 15,
      recordCount: 24,
    },
  ]

  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
      {schemas.map((schema) => (
        <Card key={schema.id}>
          <CardHeader className="space-y-1">
            <div className="flex items-center justify-between">
              <CardTitle>{schema.name}</CardTitle>
              <Badge variant="outline">{schema.fieldCount} 字段</Badge>
            </div>
            <CardDescription>{schema.description}</CardDescription>
          </CardHeader>
          <CardContent>
            <div className="text-sm">
              已有记录: <span className="font-medium">{schema.recordCount}</span>
            </div>
          </CardContent>
          <CardFooter className="flex justify-between gap-2">
            <Link href={`/data-entry/manual/${schema.id}`} className="flex-1">
              <Button variant="outline" size="sm" className="w-full">
                手动录入
              </Button>
            </Link>
            <Link href={`/data-entry/ai/${schema.id}`} className="flex-1">
              <Button size="sm" className="w-full">
                AI 辅助录入
              </Button>
            </Link>
          </CardFooter>
        </Card>
      ))}
    </div>
  )
}
