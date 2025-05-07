import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import ManualSchemaCreation from "@/components/schema/manual-schema-creation"
import AIAssistedSchemaCreation from "@/components/schema/ai-assisted-schema-creation"

export default function CreateSchema() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">创建数据结构</h1>
        <p className="text-muted-foreground">定义新的数据结构（Schema）以组织和管理您的内容</p>
      </div>

      <Tabs defaultValue="manual" className="space-y-4">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="manual">手动创建</TabsTrigger>
          <TabsTrigger value="ai-assisted">AI 辅助创建</TabsTrigger>
        </TabsList>
        <TabsContent value="manual" className="space-y-4">
          <ManualSchemaCreation />
        </TabsContent>
        <TabsContent value="ai-assisted" className="space-y-4">
          <AIAssistedSchemaCreation />
        </TabsContent>
      </Tabs>
    </div>
  )
}
