import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Eye, FileInput } from "lucide-react"
import Link from "next/link"

export default function SchemaList() {
  const schemas = [
    {
      id: "1",
      name: "游戏角色",
      description: "定义游戏中的角色属性和特征",
      fieldCount: 12,
      recordCount: 48,
      lastModified: "2023-05-01",
    },
    {
      id: "2",
      name: "游戏道具",
      description: "定义游戏中的各类道具及其属性",
      fieldCount: 8,
      recordCount: 156,
      lastModified: "2023-04-28",
    },
    {
      id: "3",
      name: "游戏任务",
      description: "定义游戏中的任务结构和奖励",
      fieldCount: 10,
      recordCount: 32,
      lastModified: "2023-04-25",
    },
    {
      id: "4",
      name: "游戏场景",
      description: "定义游戏中的场景和环境",
      fieldCount: 15,
      recordCount: 24,
      lastModified: "2023-04-20",
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
            <div className="flex justify-between text-sm">
              <div>
                记录数: <span className="font-medium">{schema.recordCount}</span>
              </div>
              <div>
                更新于: <span className="font-medium">{schema.lastModified}</span>
              </div>
            </div>
          </CardContent>
          <CardFooter className="flex justify-between gap-2">
            <Link href={`/schema-management/${schema.id}`} className="flex-1">
              <Button variant="outline" size="sm" className="w-full">
                <Eye className="mr-1 h-4 w-4" />
                查看详情
              </Button>
            </Link>
            <Link href={`/data-entry/manual/${schema.id}`} className="flex-1">
              <Button variant="outline" size="sm" className="w-full">
                <FileInput className="mr-1 h-4 w-4" />
                录入数据
              </Button>
            </Link>
          </CardFooter>
        </Card>
      ))}
    </div>
  )
}
