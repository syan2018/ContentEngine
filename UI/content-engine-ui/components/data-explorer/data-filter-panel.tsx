"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Filter, X, Plus } from "lucide-react"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Input } from "@/components/ui/input"
import { Badge } from "@/components/ui/badge"

export default function DataFilterPanel() {
  const [activeFilters, setActiveFilters] = useState<
    Array<{ id: number; field: string; operator: string; value: string }>
  >([])
  const [nextFilterId, setNextFilterId] = useState(1)

  const addFilter = (field: string, operator: string, value: string) => {
    setActiveFilters([...activeFilters, { id: nextFilterId, field, operator, value }])
    setNextFilterId(nextFilterId + 1)
  }

  const removeFilter = (id: number) => {
    setActiveFilters(activeFilters.filter((filter) => filter.id !== id))
  }

  const clearAllFilters = () => {
    setActiveFilters([])
  }

  return (
    <div className="flex flex-col gap-2">
      <Popover>
        <PopoverTrigger asChild>
          <Button variant="outline">
            <Filter className="mr-2 h-4 w-4" />
            筛选
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-80">
          <div className="space-y-4">
            <h4 className="font-medium">添加筛选条件</h4>
            <div className="space-y-2">
              <Select>
                <SelectTrigger>
                  <SelectValue placeholder="选择字段" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="name">名称</SelectItem>
                  <SelectItem value="race">种族</SelectItem>
                  <SelectItem value="class">职业</SelectItem>
                  <SelectItem value="level">等级</SelectItem>
                  <SelectItem value="health">生命值</SelectItem>
                  <SelectItem value="status">状态</SelectItem>
                </SelectContent>
              </Select>

              <Select>
                <SelectTrigger>
                  <SelectValue placeholder="选择操作符" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="equals">等于</SelectItem>
                  <SelectItem value="contains">包含</SelectItem>
                  <SelectItem value="greater_than">大于</SelectItem>
                  <SelectItem value="less_than">小于</SelectItem>
                  <SelectItem value="in">在列表中</SelectItem>
                </SelectContent>
              </Select>

              <Input placeholder="输入筛选值" />

              <Button className="w-full" onClick={() => addFilter("种族", "等于", "精灵")}>
                <Plus className="mr-2 h-4 w-4" />
                添加筛选
              </Button>
            </div>
          </div>
        </PopoverContent>
      </Popover>

      {activeFilters.length > 0 && (
        <div className="flex flex-wrap gap-2 mt-2">
          {activeFilters.map((filter) => (
            <Badge key={filter.id} variant="secondary" className="flex items-center gap-1">
              {filter.field} {filter.operator} {filter.value}
              <Button variant="ghost" size="sm" className="h-4 w-4 p-0 ml-1" onClick={() => removeFilter(filter.id)}>
                <X className="h-3 w-3" />
              </Button>
            </Badge>
          ))}

          {activeFilters.length > 1 && (
            <Button variant="ghost" size="sm" onClick={clearAllFilters} className="h-6">
              清除全部
            </Button>
          )}
        </div>
      )}
    </div>
  )
}
