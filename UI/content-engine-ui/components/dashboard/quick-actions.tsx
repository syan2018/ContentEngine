import { Button } from "@/components/ui/button"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Database, FileInput, Search, Sparkles } from "lucide-react"
import Link from "next/link"

export default function QuickActions() {
  return (
    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
      <Card>
        <CardHeader className="space-y-1">
          <CardTitle className="text-lg">创建数据结构</CardTitle>
          <CardDescription>定义新的数据结构（Schema）</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex justify-center py-4">
            <Database className="h-12 w-12 text-purple-500" />
          </div>
        </CardContent>
        <CardFooter>
          <Link href="/schema-management/create" className="w-full">
            <Button className="w-full">开始创建</Button>
          </Link>
        </CardFooter>
      </Card>

      <Card>
        <CardHeader className="space-y-1">
          <CardTitle className="text-lg">信息注入</CardTitle>
          <CardDescription>将原始信息转化为结构化数据</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex justify-center py-4">
            <FileInput className="h-12 w-12 text-purple-500" />
          </div>
        </CardContent>
        <CardFooter>
          <Link href="/data-entry" className="w-full">
            <Button className="w-full">开始注入</Button>
          </Link>
        </CardFooter>
      </Card>

      <Card>
        <CardHeader className="space-y-1">
          <CardTitle className="text-lg">数据洞察</CardTitle>
          <CardDescription>浏览和查询结构化数据内容</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex justify-center py-4">
            <Search className="h-12 w-12 text-purple-500" />
          </div>
        </CardContent>
        <CardFooter>
          <Link href="/data-explorer" className="w-full">
            <Button className="w-full">开始探索</Button>
          </Link>
        </CardFooter>
      </Card>

      <Card>
        <CardHeader className="space-y-1">
          <CardTitle className="text-lg">AI 推理</CardTitle>
          <CardDescription>基于数据进行 AI 分析与生成</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex justify-center py-4">
            <Sparkles className="h-12 w-12 text-purple-500" />
          </div>
        </CardContent>
        <CardFooter>
          <Link href="/ai-inference" className="w-full">
            <Button className="w-full">开始推理</Button>
          </Link>
        </CardFooter>
      </Card>
    </div>
  )
}
