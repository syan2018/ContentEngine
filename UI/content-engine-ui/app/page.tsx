import DashboardStats from "@/components/dashboard/dashboard-stats"
import RecentActivity from "@/components/dashboard/recent-activity"
import QuickActions from "@/components/dashboard/quick-actions"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

export default function Dashboard() {
  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight">ContentEngine 控制台</h1>
        <p className="text-muted-foreground">欢迎使用 ContentEngine，您的 AI 驱动内容结构化与推理引擎</p>
      </div>

      <DashboardStats />

      <Tabs defaultValue="quick-actions" className="space-y-4">
        <TabsList>
          <TabsTrigger value="quick-actions">快捷操作</TabsTrigger>
          <TabsTrigger value="recent-activity">最近活动</TabsTrigger>
        </TabsList>
        <TabsContent value="quick-actions" className="space-y-4">
          <QuickActions />
        </TabsContent>
        <TabsContent value="recent-activity" className="space-y-4">
          <RecentActivity />
        </TabsContent>
      </Tabs>
    </div>
  )
}
