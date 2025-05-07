"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent } from "@/components/ui/card"
import { Loader2, Sparkles } from "lucide-react"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"

export default function NaturalLanguageQuery({ schemaId }: { schemaId: string }) {
  const [query, setQuery] = useState("")
  const [isProcessing, setIsProcessing] = useState(false)
  const [results, setResults] = useState<any[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const processQuery = () => {
    if (!query.trim()) return

    setIsProcessing(true)
    setError(null)

    // 模拟 AI 处理查询
    setTimeout(() => {
      setIsProcessing(false)

      // 示例查询结果 - 在实际应用中，这会是从 AI 和数据库获取的结果
      if (query.toLowerCase().includes("精灵")) {
        setResults([
          { id: "1", name: "艾莉娅", race: "精灵", class: "法师", level: 25 },
          { id: "5", name: "莉莉安", race: "精灵", class: "德鲁伊", level: 27 },
          { id: "12", name: "伊莎贝拉", race: "精灵", class: "游侠", level: 31 },
        ])
      } else if (query.toLowerCase().includes("等级大于25")) {
        setResults([
          { id: "2", name: "托尔", race: "矮人", class: "战士", level: 30 },
          { id: "4", name: "格罗姆", race: "兽人", class: "猎人", level: 28 },
          { id: "5", name: "莉莉安", race: "精灵", class: "德鲁伊", level: 27 },
          { id: "6", name: "卡萨", race: "龙人", class: "圣骑士", level: 32 },
          { id: "10", name: "奥兰多", race: "人类", class: "武僧", level: 29 },
          { id: "11", name: "莫格拉", race: "矮人", class: "萨满", level: 27 },
          { id: "12", name: "伊莎贝拉", race: "精灵", class: "游侠", level: 31 },
        ])
      } else if (query.toLowerCase().includes("人类") && query.toLowerCase().includes("法师")) {
        setResults([])
      } else {
        setError("无法理解您的查询，请尝试更具体的描述，例如：'查找所有精灵族角色' 或 '等级大于25的角色'")
      }
    }, 1500)
  }

  return (
    <div className="space-y-4">
      <div className="space-y-2">
        <Textarea
          placeholder="用自然语言描述您的查询需求，例如：'查找所有精灵族角色' 或 '等级大于25的角色'"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          rows={3}
          className="resize-none"
        />
        <Button onClick={processQuery} disabled={!query.trim() || isProcessing} className="w-full">
          {isProcessing ? (
            <>
              <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              处理中...
            </>
          ) : (
            <>
              <Sparkles className="mr-2 h-4 w-4" />
              执行查询
            </>
          )}
        </Button>
      </div>

      {error && (
        <Card className="border-destructive">
          <CardContent className="pt-6 text-destructive">{error}</CardContent>
        </Card>
      )}

      {results && results.length > 0 && (
        <div className="rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-[80px]">ID</TableHead>
                <TableHead>名称</TableHead>
                <TableHead>种族</TableHead>
                <TableHead>职业</TableHead>
                <TableHead className="text-right">等级</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {results.map((item) => (
                <TableRow key={item.id}>
                  <TableCell className="font-medium">{item.id}</TableCell>
                  <TableCell>{item.name}</TableCell>
                  <TableCell>{item.race}</TableCell>
                  <TableCell>{item.class}</TableCell>
                  <TableCell className="text-right">{item.level}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {results && results.length === 0 && (
        <Card>
          <CardContent className="pt-6 text-center text-muted-foreground">没有找到符合条件的记录</CardContent>
        </Card>
      )}
    </div>
  )
}
