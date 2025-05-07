import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Search } from "lucide-react"
import SchemaSelectionList from "@/components/data-explorer/schema-selection-list"

export default function DataExplorer() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">数据洞察</h1>
        <p className="text-muted-foreground">浏览、查询和管理已存入的结构化数据</p>
      </div>

      <div className="flex items-center space-x-2">
        <div className="relative flex-1">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input type="search" placeholder="搜索数据结构..." className="pl-8" />
        </div>
        <Button variant="outline">筛选</Button>
      </div>

      <div>
        <h2 className="text-xl font-semibold mb-4">选择数据集合</h2>
        <SchemaSelectionList />
      </div>
    </div>
  )
}
