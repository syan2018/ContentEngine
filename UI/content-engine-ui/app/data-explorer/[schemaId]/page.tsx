import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Search, Download, Plus } from "lucide-react"
import DataTable from "@/components/data-explorer/data-table"
import DataFilterPanel from "@/components/data-explorer/data-filter-panel"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import NaturalLanguageQuery from "@/components/data-explorer/natural-language-query"
import DataVisualization from "@/components/data-explorer/data-visualization"

export default function DataExplorerView({ params }: { params: { schemaId: string } }) {
  // 这里可以根据 schemaId 获取实际的 schema 信息
  const schemaInfo = {
    id: params.schemaId,
    name: "游戏角色",
    description: "定义游戏中的角色属性和特征",
    fieldCount: 12,
    recordCount: 48,
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">{schemaInfo.name}</h1>
          <p className="text-muted-foreground">{schemaInfo.description}</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="outline" size="sm">
            <Download className="mr-2 h-4 w-4" />
            导出数据
          </Button>
          <Button size="sm">
            <Plus className="mr-2 h-4 w-4" />
            添加记录
          </Button>
        </div>
      </div>

      <Tabs defaultValue="table" className="space-y-4">
        <TabsList>
          <TabsTrigger value="table">表格视图</TabsTrigger>
          <TabsTrigger value="visualization">可视化</TabsTrigger>
          <TabsTrigger value="nlq">自然语言查询</TabsTrigger>
        </TabsList>

        <TabsContent value="table" className="space-y-4">
          <div className="flex flex-col md:flex-row gap-4">
            <div className="relative flex-1">
              <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
              <Input type="search" placeholder="搜索记录..." className="pl-8" />
            </div>
            <DataFilterPanel />
          </div>

          <DataTable schemaId={params.schemaId} />
        </TabsContent>

        <TabsContent value="visualization" className="space-y-4">
          <DataVisualization schemaId={params.schemaId} />
        </TabsContent>

        <TabsContent value="nlq" className="space-y-4">
          <NaturalLanguageQuery schemaId={params.schemaId} />
        </TabsContent>
      </Tabs>
    </div>
  )
}
