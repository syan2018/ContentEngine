import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { PlusCircle, Search } from "lucide-react"
import Link from "next/link"
import SchemaList from "@/components/schema/schema-list"

export default function SchemaManagement() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">数据管理</h1>
          <p className="text-muted-foreground">创建和管理您的数据结构定义（Schema）</p>
        </div>
        <Link href="/schema-management/create">
          <Button>
            <PlusCircle className="mr-2 h-4 w-4" />
            创建新数据结构
          </Button>
        </Link>
      </div>

      <div className="flex items-center space-x-2">
        <div className="relative flex-1">
          <Search className="absolute left-2.5 top-2.5 h-4 w-4 text-muted-foreground" />
          <Input type="search" placeholder="搜索数据结构..." className="pl-8" />
        </div>
        <Button variant="outline">筛选</Button>
      </div>

      <SchemaList />
    </div>
  )
}
