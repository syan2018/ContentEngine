"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Checkbox } from "@/components/ui/checkbox"
import { PlusCircle, Trash2 } from "lucide-react"
import { Badge } from "@/components/ui/badge"
import type { ReasoningTransactionDefinition, QueryDefinition } from "../reasoning-transaction-creation-wizard"
import { v4 as uuidv4 } from "uuid"

interface QueryDefinitionStepProps {
  transactionDefinition: ReasoningTransactionDefinition
  updateTransactionDefinition: (updates: Partial<ReasoningTransactionDefinition>) => void
  availableSchemas: { name: string; displayName: string; fieldCount: number }[]
  getSchemaFields: (schemaName: string) => { name: string; displayName: string; type: string }[]
}

export default function QueryDefinitionStep({
  transactionDefinition,
  updateTransactionDefinition,
  availableSchemas,
  getSchemaFields,
}: QueryDefinitionStepProps) {
  const [currentQuery, setCurrentQuery] = useState<QueryDefinition>({
    queryId: uuidv4(),
    outputViewName: "",
    sourceSchemaName: "",
    filterExpression: "",
    selectFields: [],
  })

  const [selectedSchema, setSelectedSchema] = useState<string>("")
  const [schemaFields, setSchemaFields] = useState<{ name: string; displayName: string; type: string }[]>([])

  const handleSchemaChange = (schemaName: string) => {
    setSelectedSchema(schemaName)
    const fields = getSchemaFields(schemaName)
    setSchemaFields(fields)
    setCurrentQuery({
      ...currentQuery,
      sourceSchemaName: schemaName,
      selectFields: [], // 重置已选字段
    })
  }

  const handleFieldToggle = (fieldName: string, checked: boolean) => {
    setCurrentQuery((prev) => {
      const newSelectFields = checked
        ? [...prev.selectFields, fieldName]
        : prev.selectFields.filter((f) => f !== fieldName)
      return { ...prev, selectFields: newSelectFields }
    })
  }

  const handleSelectAllFields = () => {
    setCurrentQuery((prev) => ({
      ...prev,
      selectFields: schemaFields.map((field) => field.name),
    }))
  }

  const handleUnselectAllFields = () => {
    setCurrentQuery((prev) => ({
      ...prev,
      selectFields: [],
    }))
  }

  const addQueryDefinition = () => {
    if (!currentQuery.outputViewName || !currentQuery.sourceSchemaName || currentQuery.selectFields.length === 0) {
      return // 验证失败
    }

    const updatedQueries = [...transactionDefinition.queryDefinitions, { ...currentQuery }]
    updateTransactionDefinition({ queryDefinitions: updatedQueries })

    // 重置当前查询
    setCurrentQuery({
      queryId: uuidv4(),
      outputViewName: "",
      sourceSchemaName: "",
      filterExpression: "",
      selectFields: [],
    })
    setSelectedSchema("")
    setSchemaFields([])
  }

  const removeQueryDefinition = (queryId: string) => {
    const updatedQueries = transactionDefinition.queryDefinitions.filter((q) => q.queryId !== queryId)
    updateTransactionDefinition({ queryDefinitions: updatedQueries })
  }

  return (
    <div className="space-y-6">
      <div className="space-y-4">
        <h3 className="text-lg font-medium">已定义的数据查询</h3>
        {transactionDefinition.queryDefinitions.length === 0 ? (
          <div className="text-center p-4 border rounded-md bg-muted">
            <p className="text-muted-foreground">尚未定义任何数据查询</p>
          </div>
        ) : (
          <div className="space-y-4">
            {transactionDefinition.queryDefinitions.map((query) => (
              <Card key={query.queryId}>
                <CardHeader className="py-4 flex flex-row items-center justify-between">
                  <CardTitle className="text-base">
                    视图: <span className="font-bold">{query.outputViewName}</span>
                  </CardTitle>
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => removeQueryDefinition(query.queryId)}
                    className="h-8 w-8 p-0"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </CardHeader>
                <CardContent className="py-2 space-y-2">
                  <div className="flex justify-between text-sm">
                    <span className="text-muted-foreground">数据源:</span>
                    <span>
                      {availableSchemas.find((s) => s.name === query.sourceSchemaName)?.displayName ||
                        query.sourceSchemaName}
                    </span>
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
        )}
      </div>

      <div className="border-t pt-6">
        <h3 className="text-lg font-medium mb-4">添加新数据查询</h3>
        <div className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="output-view-name">输出视图名称</Label>
              <Input
                id="output-view-name"
                placeholder="例如：NPCView, ScenarioView"
                value={currentQuery.outputViewName}
                onChange={(e) => setCurrentQuery({ ...currentQuery, outputViewName: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="source-schema">数据源Schema</Label>
              <Select value={selectedSchema} onValueChange={handleSchemaChange}>
                <SelectTrigger id="source-schema">
                  <SelectValue placeholder="选择数据源Schema" />
                </SelectTrigger>
                <SelectContent>
                  {availableSchemas.map((schema) => (
                    <SelectItem key={schema.name} value={schema.name}>
                      {schema.displayName} ({schema.fieldCount} 字段)
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="filter-expression">筛选条件（可选）</Label>
            <Input
              id="filter-expression"
              placeholder="例如：$.status == 'Active' && $.level > 10"
              value={currentQuery.filterExpression}
              onChange={(e) => setCurrentQuery({ ...currentQuery, filterExpression: e.target.value })}
            />
            <p className="text-xs text-muted-foreground">使用LiteDB查询语法，$表示当前文档</p>
          </div>

          {selectedSchema && (
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <Label>选择字段</Label>
                <div className="space-x-2">
                  <Button variant="outline" size="sm" onClick={handleSelectAllFields}>
                    全选
                  </Button>
                  <Button variant="outline" size="sm" onClick={handleUnselectAllFields}>
                    取消全选
                  </Button>
                </div>
              </div>
              <div className="border rounded-md p-4 max-h-60 overflow-y-auto grid grid-cols-1 md:grid-cols-2 gap-2">
                {schemaFields.map((field) => (
                  <div key={field.name} className="flex items-center space-x-2">
                    <Checkbox
                      id={`field-${field.name}`}
                      checked={currentQuery.selectFields.includes(field.name)}
                      onCheckedChange={(checked) => handleFieldToggle(field.name, checked as boolean)}
                    />
                    <Label htmlFor={`field-${field.name}`} className="flex-1 cursor-pointer">
                      {field.displayName} <span className="text-xs text-muted-foreground">({field.type})</span>
                    </Label>
                  </div>
                ))}
              </div>
            </div>
          )}

          <Button
            onClick={addQueryDefinition}
            disabled={
              !currentQuery.outputViewName || !currentQuery.sourceSchemaName || currentQuery.selectFields.length === 0
            }
            className="w-full"
          >
            <PlusCircle className="mr-2 h-4 w-4" />
            添加数据查询
          </Button>
        </div>
      </div>
    </div>
  )
}
