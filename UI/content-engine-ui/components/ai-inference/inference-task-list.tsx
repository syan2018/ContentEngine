import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Clock, Copy, Eye, Sparkles } from "lucide-react"
import Link from "next/link"

export default function InferenceTaskList() {
  const tasks = [
    {
      id: "1",
      name: "角色对话生成",
      description: "基于角色特性生成对话内容",
      status: "完成",
      createdAt: "2023-05-01 14:30",
      schemas: ["游戏角色", "游戏场景"],
      progress: { completed: 48, total: 48 },
      cost: 2.35,
    },
    {
      id: "2",
      name: "任务奖励平衡分析",
      description: "分析游戏任务奖励的平衡性",
      status: "进行中",
      createdAt: "2023-05-02 10:15",
      schemas: ["游戏任务", "游戏道具"],
      progress: { completed: 32, total: 48 },
      cost: 1.6,
    },
    {
      id: "3",
      name: "角色关系网络",
      description: "推断角色之间的社交关系网络",
      status: "暂停",
      createdAt: "2023-04-28 09:45",
      schemas: ["游戏角色"],
      progress: { completed: 15, total: 36 },
      cost: 0.75,
    },
  ]

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "完成":
        return <Badge className="bg-green-100 text-green-800 border-green-200">完成</Badge>
      case "进行中":
        return <Badge className="bg-blue-100 text-blue-800 border-blue-200">进行中</Badge>
      case "暂停":
        return <Badge className="bg-yellow-100 text-yellow-800 border-yellow-200">暂停</Badge>
      default:
        return <Badge variant="outline">{status}</Badge>
    }
  }

  return (
    <div className="space-y-4">
      {tasks.map((task) => (
        <Card key={task.id} className="hover:shadow-md transition-shadow">
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardTitle className="text-xl">{task.name}</CardTitle>
              {getStatusBadge(task.status)}
            </div>
            <CardDescription>{task.description}</CardDescription>
          </CardHeader>
          <CardContent className="pb-2">
            <div className="flex flex-wrap gap-2 mb-2">
              {task.schemas.map((schema) => (
                <Badge key={schema} variant="outline">
                  {schema}
                </Badge>
              ))}
            </div>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4 text-sm">
              <div className="flex items-center text-muted-foreground">
                <Clock className="mr-1 h-4 w-4" />
                创建于 {task.createdAt}
              </div>
              <div className="text-muted-foreground">
                进度: {task.progress.completed}/{task.progress.total} (
                {Math.round((task.progress.completed / task.progress.total) * 100)}%)
              </div>
              <div className="text-green-600 font-medium">成本: ${task.cost.toFixed(2)}</div>
            </div>
          </CardContent>
          <CardFooter className="flex justify-between gap-2">
            <Link href={`/ai-inference/${task.id}`} className="flex-1">
              <Button variant="outline" size="sm" className="w-full">
                <Eye className="mr-1 h-4 w-4" />
                查看详情
              </Button>
            </Link>
            <Button variant="outline" size="sm" className="flex-1">
              <Copy className="mr-1 h-4 w-4" />
              复制任务
            </Button>
            <Button size="sm" className="flex-1">
              <Sparkles className="mr-1 h-4 w-4" />
              重新执行
            </Button>
          </CardFooter>
        </Card>
      ))}
    </div>
  )
}
