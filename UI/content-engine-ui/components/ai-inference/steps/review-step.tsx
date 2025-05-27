"use client"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Database, ArrowRightLeft, FileText, Sparkles, Zap } from "lucide-react"
import type { ReasoningTransactionDefinition } from "../reasoning-transaction-creation-wizard"

interface ReviewStepProps {
  transactionDefinition: ReasoningTransactionDefinition
}

export default function ReviewStep({ transactionDefinition }: ReviewStepProps) {
  return (
    <div className="space-y-6">
      <Card>
        <CardHeader className="pb-3">
          <CardTitle>{transactionDefinition.name}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="whitespace-pre-wrap">{transactionDefinition.description}</p>
          {transactionDefinition.subjectSchemaName && (
            <div className="flex items-center mt-2">
              <span className="text-sm text-muted-foreground mr-2">主体Schema:</span>
              <Badge variant="outline">{transactionDefinition.subjectSchemaName}</Badge>
            </div>
          )}
        </CardContent>
      </Card>

      <Tabs defaultValue="queries" className="w-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="queries">数据查询</TabsTrigger>
          <TabsTrigger value="combination">数据组合</TabsTrigger>
          <TabsTrigger value="prompt">Prompt模板</TabsTrigger>
          <TabsTrigger value="constraints">执行约束</TabsTrigger>
        </TabsList>
        <TabsContent value="queries" className="space-y-4 pt-4">
          <h3 className="text-lg font-medium flex items-center">
            <Database className="h-5 w-5 mr-2 text-blue-500" />
            数据查询定义
          </h3>
          <div className="space-y-4">
            {transactionDefinition.queryDefinitions.map((query) => (
              <Card key={query.queryId}>
                <CardHeader className="py-3">
                  <CardTitle className="text-base">
                    视图: <span className="font-bold">{query.outputViewName}</span>
                  </CardTitle>
                </CardHeader>
                <CardContent className="py-2 space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">数据源:</span>
                    <span>{query.sourceSchemaName}</span>
                  </div>
                  {query.filterExpression && (
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">筛选条件:</span>
                      <span className="font-mono text-xs bg-muted px-2 py-1 rounded">{query.filterExpression}</span>
                    </div>
                  )}
                  <div className="text-sm">
                    <span className="text-muted-foreground">选择字段:</span>
                    <div className="flex flex-wrap gap-1 mt-1">
                      {query.selectFields.map((field) => (
                        <Badge key={field} variant="outline">
                          {field}
                        </Badge>
                      ))}
                    </div>
                  </div>
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>
        <TabsContent value="combination" className="space-y-4 pt-4">
          <h3 className="text-lg font-medium flex items-center">
            <ArrowRightLeft className="h-5 w-5 mr-2 text-purple-500" />
            数据组合规则
          </h3>
          {transactionDefinition.dataCombinationRules.map((rule, index) => (
            <Card key={index}>
              <CardHeader className="py-3">
                <CardTitle className="text-base">组合规则 #{index + 1}</CardTitle>
              </CardHeader>
              <CardContent className="py-2 space-y-4">
                <div className="space-y-2">
                  <h4 className="text-sm font-medium">叉积视图</h4>
                  {rule.viewNamesToCrossProduct.length === 0 ? (
                    <p className="text-sm text-muted-foreground">未选择任何叉积视图</p>
                  ) : (
                    <div className="flex flex-wrap gap-1">
                      {rule.viewNamesToCrossProduct.map((viewName) => (
                        <Badge key={viewName}>{viewName}</Badge>
                      ))}
                    </div>
                  )}
                </div>
                <div className="space-y-2">
                  <h4 className="text-sm font-medium">单例上下文视图</h4>
                  {rule.singletonViewNamesForContext.length === 0 ? (
                    <p className="text-sm text-muted-foreground">未选择任何单例上下文视图</p>
                  ) : (
                    <div className="flex flex-wrap gap-1">
                      {rule.singletonViewNamesForContext.map((viewName) => (
                        <Badge key={viewName} variant="outline">
                          {viewName}
                        </Badge>
                      ))}
                    </div>
                  )}
                </div>
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-1">
                    <span className="text-sm text-muted-foreground">最大组合数:</span>
                    <p className="font-medium">{rule.maxCombinations.toLocaleString()}</p>
                  </div>
                  <div className="space-y-1">
                    <span className="text-sm text-muted-foreground">组合策略:</span>
                    <p className="font-medium">
                      {rule.strategy === "CrossProduct" && "完全叉积"}
                      {rule.strategy === "RandomSampling" && "随机采样"}
                      {rule.strategy === "PrioritySampling" && "优先级采样"}
                    </p>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </TabsContent>
        <TabsContent value="prompt" className="space-y-4 pt-4">
          <h3 className="text-lg font-medium flex items-center">
            <FileText className="h-5 w-5 mr-2 text-green-500" />
            Prompt模板
          </h3>
          <Card>
            <CardContent className="p-4">
              <pre className="whitespace-pre-wrap bg-muted p-4 rounded-md text-sm overflow-auto max-h-80">
                {transactionDefinition.promptTemplate.templateContent}
              </pre>
            </CardContent>
          </Card>
          <div className="space-y-2">
            <h4 className="text-sm font-medium">预期输入视图</h4>
            <div className="flex flex-wrap gap-1">
              {transactionDefinition.promptTemplate.expectedInputViewNames.map((viewName) => (
                <Badge key={viewName}>{viewName}</Badge>
              ))}
            </div>
          </div>
        </TabsContent>
        <TabsContent value="constraints" className="space-y-4 pt-4">
          <h3 className="text-lg font-medium flex items-center">
            <Zap className="h-5 w-5 mr-2 text-amber-500" />
            执行约束
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <Card>
              <CardHeader className="py-3">
                <CardTitle className="text-base">成本控制</CardTitle>
              </CardHeader>
              <CardContent className="py-2">
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">最大成本限制:</span>
                    <span className="font-medium">
                      ${transactionDefinition.executionConstraints.maxEstimatedCostUSD}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="py-3">
                <CardTitle className="text-base">性能设置</CardTitle>
              </CardHeader>
              <CardContent className="py-2">
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">最大执行时间:</span>
                    <span className="font-medium">
                      {transactionDefinition.executionConstraints.maxExecutionTimeMinutes} 分钟
                    </span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">最大并发调用数:</span>
                    <span className="font-medium">
                      {transactionDefinition.executionConstraints.maxConcurrentAICalls}
                    </span>
                  </div>
                </div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="py-3">
                <CardTitle className="text-base">批处理设置</CardTitle>
              </CardHeader>
              <CardContent className="py-2">
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">启用批处理:</span>
                    <span className="font-medium">
                      {transactionDefinition.executionConstraints.enableBatching ? "是" : "否"}
                    </span>
                  </div>
                  {transactionDefinition.executionConstraints.enableBatching && (
                    <div className="flex justify-between">
                      <span className="text-sm text-muted-foreground">批处理大小:</span>
                      <span className="font-medium">{transactionDefinition.executionConstraints.batchSize}</span>
                    </div>
                  )}
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>
      </Tabs>

      <div className="p-4 border rounded-md bg-muted">
        <div className="flex items-center">
          <Sparkles className="h-5 w-5 mr-2 text-purple-500" />
          <h3 className="text-lg font-medium">执行预估</h3>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mt-4">
          <div className="space-y-1">
            <span className="text-sm text-muted-foreground">预估组合数:</span>
            <p className="font-bold text-lg">
              {Math.min(transactionDefinition.dataCombinationRules[0]?.maxCombinations || 1000, 1000).toLocaleString()}
            </p>
          </div>
          <div className="space-y-1">
            <span className="text-sm text-muted-foreground">预估成本:</span>
            <p className="font-bold text-lg text-green-600">
              $
              {(0.02 * Math.min(transactionDefinition.dataCombinationRules[0]?.maxCombinations || 1000, 1000)).toFixed(
                2,
              )}
            </p>
          </div>
          <div className="space-y-1">
            <span className="text-sm text-muted-foreground">预估执行时间:</span>
            <p className="font-bold text-lg">
              {Math.ceil(
                Math.min(transactionDefinition.dataCombinationRules[0]?.maxCombinations || 1000, 1000) /
                  (transactionDefinition.executionConstraints.maxConcurrentAICalls * 10),
              )}{" "}
              分钟
            </p>
          </div>
        </div>
      </div>
    </div>
  )
}
