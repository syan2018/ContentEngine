import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import AIServiceSettings from "@/components/settings/ai-service-settings"
import SystemSettings from "@/components/settings/system-settings"

export default function Settings() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">引擎配置</h1>
        <p className="text-muted-foreground">管理应用和 AI 服务的基础设置</p>
      </div>

      <Tabs defaultValue="ai-services" className="space-y-4">
        <TabsList>
          <TabsTrigger value="ai-services">AI 服务配置</TabsTrigger>
          <TabsTrigger value="system">系统参数</TabsTrigger>
        </TabsList>
        <TabsContent value="ai-services" className="space-y-4">
          <AIServiceSettings />
        </TabsContent>
        <TabsContent value="system" className="space-y-4">
          <SystemSettings />
        </TabsContent>
      </Tabs>
    </div>
  )
}
