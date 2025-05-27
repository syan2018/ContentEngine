"use client"

import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Switch } from "@/components/ui/switch"
import { Slider } from "@/components/ui/slider"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { DollarSign, Clock, Zap, Database } from "lucide-react"
import type { ReasoningTransactionDefinition } from "../reasoning-transaction-creation-wizard"

interface ExecutionConstraintsStepProps {
  transactionDefinition: ReasoningTransactionDefinition
  updateTransactionDefinition: (updates: Partial<ReasoningTransactionDefinition>) => void
}

export default function ExecutionConstraintsStep({
  transactionDefinition,
  updateTransactionDefinition,
}: ExecutionConstraintsStepProps) {
  const { executionConstraints } = transactionDefinition

  const updateConstraint = <K extends keyof typeof executionConstraints>(
    key: K,
    value: (typeof executionConstraints)[K],
  ) => {
    updateTransactionDefinition({
      executionConstraints: {
        ...executionConstraints,
        [key]: value,
      },
    })
  }

  // 估算成本（模拟）
  const estimateCost = () => {
    // 假设每个组合的成本为 $0.02
    const costPerCombination = 0.02

    // 获取理论组合数量
    let combinationCount = 1000 // 默认值
    if (transactionDefinition.dataCombinationRules.length > 0) {
      const rule = transactionDefinition.dataCombinationRules[0]
      combinationCount = Math.min(rule.maxCombinations, 1000) // 使用规则中的最大组合数或默认值
    }

    return (costPerCombination * combinationCount).toFixed(2)
  }

  return (
    <div className="space-y-6">
      <Tabs defaultValue="cost" className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="cost">成本控制</TabsTrigger>
          <TabsTrigger value="performance">性能设置</TabsTrigger>
          <TabsTrigger value="batching">批处理设置</TabsTrigger>
        </TabsList>
        <TabsContent value="cost" className="space-y-4 pt-4">
          <Card>
            <CardHeader className="pb-3">
              <CardTitle className="flex items-center">
                <DollarSign className="h-5 w-5 mr-2 text-green-600" />
                成本估算
              </CardTitle>
              <CardDescription>基于当前设置的预估执行成本</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center">
                <div className="text-3xl font-bold text-green-600">${estimateCost()}</div>
                <p className="text-sm text-muted-foreground mt-1">预估总成本</p>
              </div>
            </CardContent>
          </Card>

          <div className="space-y-2">
            <Label htmlFor="max-cost">最大成本限制 (USD)</Label>
            <div className="flex items-center gap-2">
              <span className="text-muted-foreground">$0</span>
              <Slider
                id="max-cost"
                min={1}
                max={100}
                step={1}
                value={[executionConstraints.maxEstimatedCostUSD]}
                onValueChange={(value) => updateConstraint("maxEstimatedCostUSD", value[0])}
                className="flex-1"
              />
              <span className="text-muted-foreground">$100</span>
            </div>
            <div className="flex items-center justify-between">
              <p className="text-xs text-muted-foreground">当预估成本超过此值时，系统将拒绝执行</p>
              <Input
                type="number"
                min={1}
                max={100}
                value={executionConstraints.maxEstimatedCostUSD}
                onChange={(e) => updateConstraint("maxEstimatedCostUSD", Number(e.target.value))}
                className="w-20 h-8 text-right"
              />
            </div>
          </div>
        </TabsContent>
        <TabsContent value="performance" className="space-y-4 pt-4">
          <div className="space-y-2">
            <Label htmlFor="max-execution-time">最大执行时间 (分钟)</Label>
            <div className="flex items-center gap-2">
              <Clock className="h-4 w-4 text-muted-foreground" />
              <Slider
                id="max-execution-time"
                min={1}
                max={120}
                step={1}
                value={[executionConstraints.maxExecutionTimeMinutes]}
                onValueChange={(value) => updateConstraint("maxExecutionTimeMinutes", value[0])}
                className="flex-1"
              />
              <Input
                type="number"
                min={1}
                max={120}
                value={executionConstraints.maxExecutionTimeMinutes}
                onChange={(e) => updateConstraint("maxExecutionTimeMinutes", Number(e.target.value))}
                className="w-20 h-8 text-right"
              />
            </div>
            <p className="text-xs text-muted-foreground">任务执行的最大时间限制，超过此时间将自动暂停</p>
          </div>

          <div className="space-y-2">
            <Label htmlFor="max-concurrent-calls">最大并发 AI 调用数</Label>
            <div className="flex items-center gap-2">
              <Zap className="h-4 w-4 text-muted-foreground" />
              <Slider
                id="max-concurrent-calls"
                min={1}
                max={20}
                step={1}
                value={[executionConstraints.maxConcurrentAICalls]}
                onValueChange={(value) => updateConstraint("maxConcurrentAICalls", value[0])}
                className="flex-1"
              />
              <Input
                type="number"
                min={1}
                max={20}
                value={executionConstraints.maxConcurrentAICalls}
                onChange={(e) => updateConstraint("maxConcurrentAICalls", Number(e.target.value))}
                className="w-20 h-8 text-right"
              />
            </div>
            <p className="text-xs text-muted-foreground">同时向 AI 模型发送的最大请求数，较高的值可能会导致 API 限流</p>
          </div>
        </TabsContent>
        <TabsContent value="batching" className="space-y-4 pt-4">
          <div className="flex items-center space-x-2">
            <Switch
              id="enable-batching"
              checked={executionConstraints.enableBatching}
              onCheckedChange={(checked) => updateConstraint("enableBatching", checked)}
            />
            <Label htmlFor="enable-batching">启用批处理</Label>
          </div>
          <p className="text-xs text-muted-foreground">
            批处理可以减少 API 调用次数，但可能会增加单次调用的复杂度和延迟
          </p>

          {executionConstraints.enableBatching && (
            <div className="space-y-2 pt-2">
              <Label htmlFor="batch-size">批处理大小</Label>
              <div className="flex items-center gap-2">
                <Database className="h-4 w-4 text-muted-foreground" />
                <Slider
                  id="batch-size"
                  min={1}
                  max={50}
                  step={1}
                  value={[executionConstraints.batchSize]}
                  onValueChange={(value) => updateConstraint("batchSize", value[0])}
                  className="flex-1"
                />
                <Input
                  type="number"
                  min={1}
                  max={50}
                  value={executionConstraints.batchSize}
                  onChange={(e) => updateConstraint("batchSize", Number(e.target.value))}
                  className="w-20 h-8 text-right"
                />
              </div>
              <p className="text-xs text-muted-foreground">
                每批处理的组合数量，较大的值可以减少 API 调用次数，但可能会增加单次调用的复杂度
              </p>
            </div>
          )}
        </TabsContent>
      </Tabs>

      <div className="space-y-4 pt-4 border-t">
        <h3 className="text-lg font-medium">执行约束说明</h3>
        <p className="text-sm text-muted-foreground">
          执行约束用于控制推理事务的资源消耗和风险。系统会在执行前估算成本和时间，并在执行过程中实时监控这些指标。
          当预估或实际消耗超过设定的限制时，系统将拒绝执行或自动暂停任务。
        </p>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Card>
            <CardHeader className="py-3">
              <CardTitle className="text-base">成本控制</CardTitle>
            </CardHeader>
            <CardContent className="py-2">
              <p className="text-sm">
                设置最大成本限制可以防止意外的高额费用。系统会在执行前估算总成本，并在执行过程中跟踪实际消耗。
              </p>
            </CardContent>
          </Card>
          <Card>
            <CardHeader className="py-3">
              <CardTitle className="text-base">性能设置</CardTitle>
            </CardHeader>
            <CardContent className="py-2">
              <p className="text-sm">
                控制执行时间和并发调用数可以优化系统性能和响应时间。较高的并发数可以加快执行速度，但可能会导致 API
                限流。
              </p>
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  )
}
