import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar"

export default function RecentActivity() {
  const activities = [
    {
      id: 1,
      user: "管理员",
      action: "创建了新的数据结构",
      target: "游戏角色",
      time: "10 分钟前",
      avatar: "/diverse-group-avatars.png",
    },
    {
      id: 2,
      user: "策划小王",
      action: "添加了新的数据记录",
      target: "精灵族角色：艾莉娅",
      time: "30 分钟前",
      avatar: "/diverse-group-avatars.png",
    },
    {
      id: 3,
      user: "设计师小李",
      action: "执行了 AI 推理任务",
      target: "角色对话生成",
      time: "1 小时前",
      avatar: "/diverse-group-avatars.png",
    },
    {
      id: 4,
      user: "程序员小张",
      action: "修改了数据结构",
      target: "游戏道具",
      time: "2 小时前",
      avatar: "/diverse-group-avatars.png",
    },
    {
      id: 5,
      user: "管理员",
      action: "更新了 AI 服务配置",
      target: "OpenAI API 设置",
      time: "3 小时前",
      avatar: "/diverse-group-avatars.png",
    },
  ]

  return (
    <Card>
      <CardHeader>
        <CardTitle>最近活动</CardTitle>
        <CardDescription>系统中的最新操作和变更</CardDescription>
      </CardHeader>
      <CardContent>
        <div className="space-y-4">
          {activities.map((activity) => (
            <div key={activity.id} className="flex items-center gap-4">
              <Avatar className="h-8 w-8">
                <AvatarImage src={activity.avatar || "/placeholder.svg"} alt={activity.user} />
                <AvatarFallback>{activity.user[0]}</AvatarFallback>
              </Avatar>
              <div className="flex-1 space-y-1">
                <p className="text-sm font-medium leading-none">
                  {activity.user} {activity.action}
                </p>
                <p className="text-sm text-muted-foreground">{activity.target}</p>
              </div>
              <div className="text-sm text-muted-foreground">{activity.time}</div>
            </div>
          ))}
        </div>
      </CardContent>
    </Card>
  )
}
