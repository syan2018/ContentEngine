"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent } from "@/components/ui/card"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { ArrowRightLeft, AlertCircle, Info } from "lucide-react"
import type { ReasoningTransactionDefinition, DataCombinationRule } from "../reasoning-transaction-creation-wizard"

interface DataCombinationStepProps {
  transactionDefinition: ReasoningTransactionDefinition
  updateTransactionDefinition: (updates: Partial<ReasoningTransactionDefinition>) => void
}

export default function DataCombinationStep({
  transactionDefinition,
  updateTransactionDefinition,
}: DataCombinationStepProps) {
  // 获取所有可用的视图名称
  const availableViews = transactionDefinition.queryDefinitions.map((query) => query.outputViewName)

  // 当前编辑的组合规则（默认使用第一个规则）
  const [currentRule, setCurrentRule] = useState<DataCombinationRule>(
    transactionDefinition.dataCombinationRules[0] || {
      viewNamesToCrossProduct: [],
      singletonViewNamesForContext: [],
      maxCombinations: 1000,
      strategy: "CrossProduct",
    },
  )

  // 当视图定义变化时，更新可用视图列表
  useEffect(() => {
    // Only update if the available views have changed
    const updatedCrossProduct = currentRule.viewNamesToCrossProduct.filter((view) => availableViews.includes(view))
    const updatedSingletons = currentRule.singletonViewNamesForContext.filter((view) => availableViews.includes(view))

    // Check if arrays have actually changed before updating state
    const crossProductChanged =
      updatedCrossProduct.length !== currentRule.viewNamesToCrossProduct.length ||
      !updatedCrossProduct.every((view, i) => currentRule.viewNamesToCrossProduct[i] === view)

    const singletonsChanged =
      updatedSingletons.length !== currentRule.singletonViewNamesForContext.length ||
      !updatedSingletons.every((view, i) => currentRule.singletonViewNamesForContext[i] === view)

    if (crossProductChanged || singletonsChanged) {
      const updatedRule = {
        ...currentRule,
        viewNamesToCrossProduct: updatedCrossProduct,
        singletonViewNamesForContext: updatedSingletons,
      }
      setCurrentRule(updatedRule)
      // Don't call updateCombinationRule here to avoid the loop
      // We'll update the parent state in a separate useEffect
    }
  }, [availableViews, currentRule.viewNamesToCrossProduct, currentRule.singletonViewNamesForContext])

  // Add a separate useEffect to update the parent state
  useEffect(() => {
    // This will only run when currentRule changes, not on every render
    updateCombinationRule(currentRule)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentRule])

  // 更新组合规则
  const updateCombinationRule = (rule: DataCombinationRule) => {
    const updatedRules = [...transactionDefinition.dataCombinationRules]
    updatedRules[0] = rule
    updateTransactionDefinition({ dataCombinationRules: updatedRules })
  }

  // 添加视图到叉积列表
  const addToCrossProduct = (viewName: string) => {
    if (currentRule.singletonViewNamesForContext.includes(viewName)) {
      // 如果视图已在单例列表中，先从单例列表移除
      const updatedSingletons = currentRule.singletonViewNamesForContext.filter((v) => v !== viewName)
      const updatedRule = {
        ...currentRule,
        viewNamesToCrossProduct: [...currentRule.viewNamesToCrossProduct, viewName],
        singletonViewNamesForContext: updatedSingletons,
      }
      setCurrentRule(updatedRule)
      updateCombinationRule(updatedRule)
    } else if (!currentRule.viewNamesToCrossProduct.includes(viewName)) {
      // 如果视图不在叉积列表中，添加到叉积列表
      const updatedRule = {
        ...currentRule,
        viewNamesToCrossProduct: [...currentRule.viewNamesToCrossProduct, viewName],
      }
      setCurrentRule(updatedRule)
      updateCombinationRule(updatedRule)
    }
  }

  // 添加视图到单例上下文列表
  const addToSingletonContext = (viewName: string) => {
    if (currentRule.viewNamesToCrossProduct.includes(viewName)) {
      // 如果视图已在叉积列表中，先从叉积列表移除
      const updatedCrossProduct = currentRule.viewNamesToCrossProduct.filter((v) => v !== viewName)
      const updatedRule = {
        ...currentRule,
        viewNamesToCrossProduct: updatedCrossProduct,
        singletonViewNamesForContext: [...currentRule.singletonViewNamesForContext, viewName],
      }
      setCurrentRule(updatedRule)
      updateCombinationRule(updatedRule)
    } else if (!currentRule.singletonViewNamesForContext.includes(viewName)) {
      // 如果视图不在单例列表中，添加到单例列表
      const updatedRule = {
        ...currentRule,
        singletonViewNamesForContext: [...currentRule.singletonViewNamesForContext, viewName],
      }
      setCurrentRule(updatedRule)
      updateCombinationRule(updatedRule)
    }
  }

  // 从叉积列表移除视图
  const removeFromCrossProduct = (viewName: string) => {
    const updatedRule = {
      ...currentRule,
      viewNamesToCrossProduct: currentRule.viewNamesToCrossProduct.filter((v) => v !== viewName),
    }
    setCurrentRule(updatedRule)
    updateCombinationRule(updatedRule)
  }

  // 从单例上下文列表移除视图
  const removeFromSingletonContext = (viewName: string) => {
    const updatedRule = {
      ...currentRule,
      singletonViewNamesForContext: currentRule.singletonViewNamesForContext.filter((v) => v !== viewName),
    }
    setCurrentRule(updatedRule)
    updateCombinationRule(updatedRule)
  }

  // 更新最大组合数
  const updateMaxCombinations = (value: string) => {
    const maxCombinations = Number.parseInt(value) || 1000
    const updatedRule = { ...currentRule, maxCombinations }
    setCurrentRule(updatedRule)
    updateCombinationRule(updatedRule)
  }

  // 更新组合策略
  const updateStrategy = (strategy: "CrossProduct" | "RandomSampling" | "PrioritySampling") => {
    const updatedRule = { ...currentRule, strategy }
    setCurrentRule(updatedRule)
    updateCombinationRule(updatedRule)
  }

  // 更新采样规则
  const updateSamplingRule = (updates: Partial<NonNullable<DataCombinationRule["samplingRule"]>>) => {
    const currentSamplingRule = currentRule.samplingRule || {
      priorityField: "Priority",
      preferHigherValues: true,
      randomSeed: 0.5,
    }
    const updatedSamplingRule = { ...currentSamplingRule, ...updates }
    const updatedRule = { ...currentRule, samplingRule: updatedSamplingRule }
    setCurrentRule(updatedRule)
    updateCombinationRule(updatedRule)
  }

  // 计算理论组合数
  const calculateTheoreticalCombinations = () => {
    if (currentRule.viewNamesToCrossProduct.length === 0) return 0

    // 假设每个视图有5个条目（实际应用中应该从数据库获取真实数量）
    const assumedItemsPerView = 5
    return Math.pow(assumedItemsPerView, currentRule.viewNamesToCrossProduct.length)
  }

  // 获取未分配的视图
  const getUnassignedViews = () => {
    return availableViews.filter(
      (view) =>
        !currentRule.viewNamesToCrossProduct.includes(view) && !currentRule.singletonViewNamesForContext.includes(view),
    )
  }

  const theoreticalCombinations = calculateTheoreticalCombinations()
  const unassignedViews = getUnassignedViews()

  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label>数据组合规则</Label>
          {theoreticalCombinations > currentRule.maxCombinations && (
            <div className="flex items-center text-amber-500">
              <AlertCircle className="h-4 w-4 mr-1" />
              <span className="text-xs">组合数量超过限制，将应用采样策略</span>
            </div>
          )}
        </div>

        <Card>
          <CardContent className="p-4 space-y-4">
            <div className="flex items-center justify-between">
              <div className="flex items-center">
                <ArrowRightLeft className="h-5 w-5 mr-2 text-purple-500" />
                <span className="font-medium">叉积视图</span>
              </div>
              <Badge variant="outline">{currentRule.viewNamesToCrossProduct.length} 个视图</Badge>
            </div>
            {currentRule.viewNamesToCrossProduct.length === 0 ? (
              <div className="text-center p-2 border rounded-md bg-muted">
                <p className="text-muted-foreground text-sm">尚未选择任何叉积视图</p>
              </div>
            ) : (
              <div className="flex flex-wrap gap-2">
                {currentRule.viewNamesToCrossProduct.map((viewName) => (
                  <Badge key={viewName} className="flex items-center gap-1 px-3 py-1">
                    {viewName}
                    <button
                      className="ml-1 text-xs hover:text-destructive"
                      onClick={() => removeFromCrossProduct(viewName)}
                    >
                      ×
                    </button>
                  </Badge>
                ))}
              </div>
            )}

            <div className="flex items-center justify-between">
              <div className="flex items-center">
                <Info className="h-5 w-5 mr-2 text-blue-500" />
                <span className="font-medium">单例上下文视图</span>
              </div>
              <Badge variant="outline">{currentRule.singletonViewNamesForContext.length} 个视图</Badge>
            </div>
            {currentRule.singletonViewNamesForContext.length === 0 ? (
              <div className="text-center p-2 border rounded-md bg-muted">
                <p className="text-muted-foreground text-sm">尚未选择任何单例上下文视图</p>
              </div>
            ) : (
              <div className="flex flex-wrap gap-2">
                {currentRule.singletonViewNamesForContext.map((viewName) => (
                  <Badge key={viewName} variant="outline" className="flex items-center gap-1 px-3 py-1">
                    {viewName}
                    <button
                      className="ml-1 text-xs hover:text-destructive"
                      onClick={() => removeFromSingletonContext(viewName)}
                    >
                      ×
                    </button>
                  </Badge>
                ))}
              </div>
            )}
          </CardContent>
        </Card>
      </div>

      {unassignedViews.length > 0 && (
        <div className="space-y-2">
          <Label>未分配的视图</Label>
          <div className="flex flex-wrap gap-2">
            {unassignedViews.map((viewName) => (
              <div key={viewName} className="flex items-center gap-2">
                <Badge variant="secondary" className="px-3 py-1">
                  {viewName}
                </Badge>
                <div className="flex gap-1">
                  <Button variant="outline" size="sm" className="h-7 px-2" onClick={() => addToCrossProduct(viewName)}>
                    添加到叉积
                  </Button>
                  <Button
                    variant="outline"
                    size="sm"
                    className="h-7 px-2"
                    onClick={() => addToSingletonContext(viewName)}
                  >
                    添加到单例
                  </Button>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="space-y-4 pt-4 border-t">
        <h3 className="text-lg font-medium">高级设置</h3>

        <Tabs defaultValue="basic" className="w-full">
          <TabsList className="grid w-full grid-cols-2">
            <TabsTrigger value="basic">基本设置</TabsTrigger>
            <TabsTrigger value="advanced">采样策略</TabsTrigger>
          </TabsList>
          <TabsContent value="basic" className="space-y-4 pt-4">
            <div className="space-y-2">
              <Label htmlFor="max-combinations">最大组合数量</Label>
              <Input
                id="max-combinations"
                type="number"
                min="1"
                value={currentRule.maxCombinations}
                onChange={(e) => updateMaxCombinations(e.target.value)}
              />
              <p className="text-xs text-muted-foreground">
                限制生成的最大组合数量，防止组合爆炸。当理论组合数超过此值时，将应用采样策略。
              </p>
            </div>

            <div className="space-y-2">
              <Label>理论组合数量</Label>
              <div className="p-2 border rounded-md bg-muted">
                <p className="text-center font-mono">
                  {theoreticalCombinations.toLocaleString()} 个组合
                  {theoreticalCombinations > 0 &&
                    ` (假设每个视图有 5 个条目，${currentRule.viewNamesToCrossProduct.length} 个视图叉积)`}
                </p>
              </div>
            </div>
          </TabsContent>
          <TabsContent value="advanced" className="space-y-4 pt-4">
            <div className="space-y-2">
              <Label htmlFor="combination-strategy">组合策略</Label>
              <Select value={currentRule.strategy} onValueChange={(value) => updateStrategy(value as any)}>
                <SelectTrigger id="combination-strategy">
                  <SelectValue placeholder="选择组合策略" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="CrossProduct">完全叉积</SelectItem>
                  <SelectItem value="RandomSampling">随机采样</SelectItem>
                  <SelectItem value="PrioritySampling">优先级采样</SelectItem>
                </SelectContent>
              </Select>
              <p className="text-xs text-muted-foreground">
                当理论组合数超过最大限制时，系统将使用此策略来减少组合数量。
              </p>
            </div>

            {currentRule.strategy !== "CrossProduct" && (
              <div className="space-y-4">
                {currentRule.strategy === "PrioritySampling" && (
                  <div className="space-y-2">
                    <Label htmlFor="priority-field">优先级字段</Label>
                    <Input
                      id="priority-field"
                      value={currentRule.samplingRule?.priorityField || "Priority"}
                      onChange={(e) => updateSamplingRule({ priorityField: e.target.value })}
                    />
                    <p className="text-xs text-muted-foreground">
                      用于排序的字段名，系统将根据此字段的值进行优先级排序。
                    </p>
                  </div>
                )}

                {currentRule.strategy === "PrioritySampling" && (
                  <div className="flex items-center space-x-2">
                    <Switch
                      id="prefer-higher-values"
                      checked={currentRule.samplingRule?.preferHigherValues ?? true}
                      onCheckedChange={(checked) => updateSamplingRule({ preferHigherValues: checked })}
                    />
                    <Label htmlFor="prefer-higher-values">优先选择高值</Label>
                  </div>
                )}

                {currentRule.strategy === "RandomSampling" && (
                  <div className="space-y-2">
                    <Label htmlFor="random-seed">随机种子</Label>
                    <Input
                      id="random-seed"
                      type="number"
                      min="0"
                      max="1"
                      step="0.1"
                      value={currentRule.samplingRule?.randomSeed || 0.5}
                      onChange={(e) => updateSamplingRule({ randomSeed: Number.parseFloat(e.target.value) || 0.5 })}
                    />
                    <p className="text-xs text-muted-foreground">
                      随机采样的种子值，范围 0-1。相同的种子值将产生相同的采样结果。
                    </p>
                  </div>
                )}
              </div>
            )}
          </TabsContent>
        </Tabs>
      </div>
    </div>
  )
}
