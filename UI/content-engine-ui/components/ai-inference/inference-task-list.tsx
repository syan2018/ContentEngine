import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Clock, Copy, Eye, Sparkles } from "lucide-react"

export default function InferenceTaskList() {
  const tasks = [
    {
      id: 1,
      name: "角色对话生成",
      description: "基于角色特性生成对话内容",
      status: "完成",
      createdAt: "2023-05-01 14:30",
      schemas: ["游戏角色"],
    },
    {
      id: 2,
      name: "任务奖励平衡分析",
      description: "分析游戏任务奖励的平衡性",
      status: "进行中",
      createdAt: "2023-05-02 10:15",
      schemas: ["游戏任务", "游戏道具"],
    },
    {
      id: 3,
      name: "角色关系网络",
      description: "推断角色之间的社交关系网络",
      status: "完成",
      createdAt: "2023-04-28 09:45",
      schemas: ["游戏角色"],
    },
  ]

  return (
    <div className="space-y-4">
      {tasks.map((task) => (
        <Card key={task.id}>
          <CardHeader className="pb-2">
            <div className="flex items-center justify-between">
              <CardTitle className="text-xl">{task.name}</CardTitle>
              <Badge variant={task.status === "完成" ? "default" : "secondary"} className="ml-2">
                {task.status}
              </Badge>
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
            <div className="flex items-center text-sm text-muted-foreground">
              <Clock className="mr-1 h-4 w-4" />
              创建于 {task.createdAt}
            </div>
          </CardContent>
          <CardFooter className="flex justify-between gap-2">
            <Button variant="outline" size="sm" className="flex-1">
              <Eye className="mr-1 h-4 w-4" />
              查看结果
            </Button>
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
