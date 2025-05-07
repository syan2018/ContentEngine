"use client"

import { useState } from "react"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Button } from "@/components/ui/button"
import {
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  Eye,
  Edit,
  Trash2,
  ArrowUpDown,
  MoreHorizontal,
} from "lucide-react"
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu"
import { Badge } from "@/components/ui/badge"
import Link from "next/link"

export default function DataTable({ schemaId }: { schemaId: string }) {
  const [currentPage, setCurrentPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)

  // 模拟数据 - 在实际应用中，这些数据会从API获取
  const gameCharacters = [
    {
      id: "1",
      name: "艾莉娅",
      race: "精灵",
      class: "法师",
      level: 25,
      health: 850,
      status: "活跃",
    },
    {
      id: "2",
      name: "托尔",
      race: "矮人",
      class: "战士",
      level: 30,
      health: 1200,
      status: "活跃",
    },
    {
      id: "3",
      name: "莱克斯",
      race: "人类",
      class: "盗贼",
      level: 22,
      health: 780,
      status: "休息",
    },
    {
      id: "4",
      name: "格罗姆",
      race: "兽人",
      class: "猎人",
      level: 28,
      health: 950,
      status: "任务中",
    },
    {
      id: "5",
      name: "莉莉安",
      race: "精灵",
      class: "德鲁伊",
      level: 27,
      health: 920,
      status: "活跃",
    },
    {
      id: "6",
      name: "卡萨",
      race: "龙人",
      class: "圣骑士",
      level: 32,
      health: 1350,
      status: "活跃",
    },
    {
      id: "7",
      name: "维克多",
      race: "人类",
      class: "牧师",
      level: 24,
      health: 820,
      status: "休息",
    },
    {
      id: "8",
      name: "塞拉",
      race: "暗精灵",
      class: "术士",
      level: 26,
      health: 750,
      status: "任务中",
    },
    {
      id: "9",
      name: "布鲁姆",
      race: "侏儒",
      class: "工程师",
      level: 23,
      health: 680,
      status: "活跃",
    },
    {
      id: "10",
      name: "奥兰多",
      race: "人类",
      class: "武僧",
      level: 29,
      health: 980,
      status: "活跃",
    },
    {
      id: "11",
      name: "莫格拉",
      race: "矮人",
      class: "萨满",
      level: 27,
      health: 900,
      status: "休息",
    },
    {
      id: "12",
      name: "伊莎贝拉",
      race: "精灵",
      class: "游侠",
      level: 31,
      health: 1050,
      status: "任务中",
    },
  ]

  const totalPages = Math.ceil(gameCharacters.length / pageSize)
  const startIndex = (currentPage - 1) * pageSize
  const endIndex = startIndex + pageSize
  const currentData = gameCharacters.slice(startIndex, endIndex)

  const getStatusBadgeVariant = (status: string) => {
    switch (status) {
      case "活跃":
        return "default"
      case "休息":
        return "secondary"
      case "任务中":
        return "outline"
      default:
        return "default"
    }
  }

  return (
    <div className="space-y-4">
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-[80px]">ID</TableHead>
              <TableHead>
                <div className="flex items-center">
                  名称
                  <ArrowUpDown className="ml-2 h-4 w-4" />
                </div>
              </TableHead>
              <TableHead>种族</TableHead>
              <TableHead>职业</TableHead>
              <TableHead className="text-right">等级</TableHead>
              <TableHead className="text-right">生命值</TableHead>
              <TableHead>状态</TableHead>
              <TableHead className="w-[100px]">操作</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {currentData.map((character) => (
              <TableRow key={character.id}>
                <TableCell className="font-medium">{character.id}</TableCell>
                <TableCell>{character.name}</TableCell>
                <TableCell>{character.race}</TableCell>
                <TableCell>{character.class}</TableCell>
                <TableCell className="text-right">{character.level}</TableCell>
                <TableCell className="text-right">{character.health}</TableCell>
                <TableCell>
                  <Badge variant={getStatusBadgeVariant(character.status)}>{character.status}</Badge>
                </TableCell>
                <TableCell>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" className="h-8 w-8 p-0">
                        <span className="sr-only">打开菜单</span>
                        <MoreHorizontal className="h-4 w-4" />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuLabel>操作</DropdownMenuLabel>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem asChild>
                        <Link href={`/data-explorer/${schemaId}/${character.id}`}>
                          <Eye className="mr-2 h-4 w-4" />
                          查看
                        </Link>
                      </DropdownMenuItem>
                      <DropdownMenuItem>
                        <Edit className="mr-2 h-4 w-4" />
                        编辑
                      </DropdownMenuItem>
                      <DropdownMenuItem className="text-destructive">
                        <Trash2 className="mr-2 h-4 w-4" />
                        删除
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <div className="flex items-center justify-between">
        <div className="text-sm text-muted-foreground">
          显示 {startIndex + 1}-{Math.min(endIndex, gameCharacters.length)} 条，共 {gameCharacters.length} 条
        </div>
        <div className="flex items-center space-x-2">
          <Button variant="outline" size="sm" onClick={() => setCurrentPage(1)} disabled={currentPage === 1}>
            <ChevronsLeft className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setCurrentPage(currentPage - 1)}
            disabled={currentPage === 1}
          >
            <ChevronLeft className="h-4 w-4" />
          </Button>
          <div className="text-sm">
            第 {currentPage} 页，共 {totalPages} 页
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setCurrentPage(currentPage + 1)}
            disabled={currentPage === totalPages}
          >
            <ChevronRight className="h-4 w-4" />
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setCurrentPage(totalPages)}
            disabled={currentPage === totalPages}
          >
            <ChevronsRight className="h-4 w-4" />
          </Button>
        </div>
      </div>
    </div>
  )
}
