"use client"

import { useState } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

export default function DataVisualization({ schemaId }: { schemaId: string }) {
  const [chartType, setChartType] = useState("bar")
  const [dataField, setDataField] = useState("race")

  // 在实际应用中，这些数据会从API获取
  const visualizationData = {
    race: {
      labels: ["精灵", "人类", "矮人", "兽人", "暗精灵", "龙人", "侏儒"],
      values: [3, 3, 2, 1, 1, 1, 1],
    },
    class: {
      labels: ["法师", "战士", "盗贼", "猎人", "德鲁伊", "圣骑士", "牧师", "术士", "工程师", "武僧", "萨满", "游侠"],
      values: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1],
    },
    level: {
      labels: ["20-24", "25-29", "30+"],
      values: [3, 6, 3],
    },
    status: {
      labels: ["活跃", "休息", "任务中"],
      values: [6, 3, 3],
    },
  }

  // 模拟图表渲染 - 在实际应用中，您可能会使用 Chart.js, D3.js 或其他图表库
  const renderChart = () => {
    const data = visualizationData[dataField as keyof typeof visualizationData]

    if (chartType === "bar") {
      return (
        <div className="h-80 flex items-end justify-around p-4">
          {data.labels.map((label, index) => {
            const height = (data.values[index] / Math.max(...data.values)) * 100
            return (
              <div key={label} className="flex flex-col items-center">
                <div className="w-16 bg-purple-500 rounded-t-md" style={{ height: `${height}%` }}></div>
                <div className="mt-2 text-sm text-center">{label}</div>
                <div className="text-sm font-bold">{data.values[index]}</div>
              </div>
            )
          })}
        </div>
      )
    }

    if (chartType === "pie") {
      // 简化的饼图表示 - 在实际应用中使用真实的饼图组件
      return (
        <div className="h-80 flex items-center justify-center p-4">
          <div className="relative w-64 h-64 rounded-full overflow-hidden">
            {data.labels.map((label, index) => {
              const percentage = (data.values[index] / data.values.reduce((a, b) => a + b, 0)) * 100
              const colors = [
                "bg-purple-500",
                "bg-blue-500",
                "bg-green-500",
                "bg-yellow-500",
                "bg-red-500",
                "bg-indigo-500",
                "bg-pink-500",
                "bg-orange-500",
              ]
              return (
                <div key={label} className="absolute inset-0 flex items-center justify-center">
                  <div
                    className={`absolute inset-0 ${colors[index % colors.length]}`}
                    style={{
                      clipPath: `polygon(50% 50%, 50% 0%, ${50 + 50 * Math.cos(2 * Math.PI * (percentage / 100))}% ${50 - 50 * Math.sin(2 * Math.PI * (percentage / 100))}%, 50% 50%)`,
                      transform: `rotate(${(index * 360) / data.labels.length}deg)`,
                    }}
                  ></div>
                </div>
              )
            })}
          </div>
        </div>
      )
    }

    return <div className="h-80 flex items-center justify-center text-muted-foreground">请选择图表类型和数据字段</div>
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <div className="space-y-2">
          <label className="text-sm font-medium">图表类型</label>
          <Select value={chartType} onValueChange={setChartType}>
            <SelectTrigger>
              <SelectValue placeholder="选择图表类型" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="bar">柱状图</SelectItem>
              <SelectItem value="pie">饼图</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <label className="text-sm font-medium">数据字段</label>
          <Select value={dataField} onValueChange={setDataField}>
            <SelectTrigger>
              <SelectValue placeholder="选择数据字段" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="race">种族分布</SelectItem>
              <SelectItem value="class">职业分布</SelectItem>
              <SelectItem value="level">等级分布</SelectItem>
              <SelectItem value="status">状态分布</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>
            {dataField === "race" && "种族分布"}
            {dataField === "class" && "职业分布"}
            {dataField === "level" && "等级分布"}
            {dataField === "status" && "状态分布"}
          </CardTitle>
          <CardDescription>
            {chartType === "bar" && "柱状图展示"}
            {chartType === "pie" && "饼图展示"}
          </CardDescription>
        </CardHeader>
        <CardContent>{renderChart()}</CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>数据摘要</CardTitle>
          <CardDescription>当前数据集的统计信息</CardDescription>
        </CardHeader>
        <CardContent>
          <Tabs defaultValue="summary" className="space-y-4">
            <TabsList>
              <TabsTrigger value="summary">摘要</TabsTrigger>
              <TabsTrigger value="details">详情</TabsTrigger>
            </TabsList>
            <TabsContent value="summary">
              <dl className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">记录总数</dt>
                  <dd className="text-2xl font-bold">12</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">平均等级</dt>
                  <dd className="text-2xl font-bold">27.1</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">最高等级</dt>
                  <dd className="text-2xl font-bold">32</dd>
                </div>
              </dl>
            </TabsContent>
            <TabsContent value="details">
              <div className="space-y-4">
                <div>
                  <h4 className="text-sm font-medium mb-2">种族分布</h4>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
                    {visualizationData.race.labels.map((label, index) => (
                      <div key={label} className="flex justify-between">
                        <span>{label}:</span>
                        <span className="font-medium">{visualizationData.race.values[index]}</span>
                      </div>
                    ))}
                  </div>
                </div>
                <div>
                  <h4 className="text-sm font-medium mb-2">状态分布</h4>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-2">
                    {visualizationData.status.labels.map((label, index) => (
                      <div key={label} className="flex justify-between">
                        <span>{label}:</span>
                        <span className="font-medium">{visualizationData.status.values[index]}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </div>
  )
}
