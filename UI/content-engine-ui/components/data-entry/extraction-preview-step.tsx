"use client"

import { useState, useEffect } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import { Loader2, FileText, LinkIcon, Type, AlertCircle, CheckCircle, RefreshCw } from "lucide-react"
import type { DataSource, ExtractionResult } from "./ai-data-entry-wizard"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"

interface ExtractionPreviewStepProps {
  schema: {
    id: string
    name: string
    description: string
    fields: {
      id: number
      name: string
      displayName: string
      type: string
      required: boolean
    }[]
  }
  dataSources: DataSource[]
  fieldMappings: Record<string, string>
  extractionMode: "one-to-one" | "batch"
  extractionResults: ExtractionResult[]
  onComplete: (results: ExtractionResult[]) => void
  isExtracting: boolean
  startExtraction: () => void
}

export default function ExtractionPreviewStep({
  schema,
  dataSources,
  fieldMappings,
  extractionMode,
  extractionResults,
  onComplete,
  isExtracting,
  startExtraction,
}: ExtractionPreviewStepProps) {
  const [results, setResults] = useState<ExtractionResult[]>(extractionResults)
  const [activeSourceId, setActiveSourceId] = useState<string | null>(dataSources.length > 0 ? dataSources[0].id : null)
  const [activeRecordIndex, setActiveRecordIndex] = useState(0)
  const [editedRecord, setEditedRecord] = useState<Record<string, any> | null>(null)

  // 移除在 useEffect 中对 extractionResults 的监听，以防止循环更新
  // 替换现有的 useEffect 为以下代码
  useEffect(() => {
    // 当外部结果更新时同步本地状态
    setResults(extractionResults)

    // 如果有成功提取的结果，设置第一个作为活动记录
    const firstSuccessResult = extractionResults.find((r) => r.status === "success" && r.records.length > 0)
    if (firstSuccessResult) {
      setActiveSourceId(firstSuccessResult.sourceId)
      setActiveRecordIndex(0)
      setEditedRecord(firstSuccessResult.records[0])
    } else if (activeSourceId) {
      // 保持当前选中的数据源
      const activeResult = extractionResults.find((r) => r.sourceId === activeSourceId)
      if (activeResult && activeResult.status === "success" && activeResult.records.length > 0) {
        setActiveRecordIndex(0)
        setEditedRecord(activeResult.records[0])
      }
    } else {
      // 初始化activeSourceId
      const firstSource = dataSources.length > 0 ? dataSources[0].id : null
      setActiveSourceId(firstSource)
    }
  }, [extractionResults, activeSourceId, dataSources])

  const handleRetryExtraction = (sourceId: string) => {
    // 更新本地状态
    setResults(
      results.map((result) =>
        result.sourceId === sourceId
          ? {
              ...result,
              status: "pending",
              records: [],
              error: undefined,
            }
          : result,
      ),
    )

    // 通知父组件
    onComplete(
      results.map((result) =>
        result.sourceId === sourceId
          ? {
              ...result,
              status: "pending",
              records: [],
              error: undefined,
            }
          : result,
      ),
    )

    // 开始提取
    startExtraction()
  }

  const handleRetryAll = () => {
    // 更新本地状态
    const resetResults = results.map((result) => ({
      ...result,
      status: "pending",
      records: [],
      error: undefined,
    }))

    setResults(resetResults)

    // 通知父组件
    onComplete(resetResults)

    // 开始提取
    startExtraction()
  }

  const handleRecordChange = (fieldName: string, value: any) => {
    if (!editedRecord) return

    setEditedRecord({
      ...editedRecord,
      [fieldName]: value,
    })

    // 更新结果中的记录
    const updatedResults = results.map((result) =>
      result.sourceId === activeSourceId
        ? {
            ...result,
            records: result.records.map((record, idx) =>
              idx === activeRecordIndex ? { ...record, [fieldName]: value } : record,
            ),
          }
        : result,
    )

    setResults(updatedResults)

    // 通知父组件
    onComplete(updatedResults)
  }

  const handleComplete = () => {
    onComplete(results)
  }

  const activeSource = dataSources.find((s) => s.id === activeSourceId)
  const activeResult = results.find((r) => r.sourceId === activeSourceId)
  const activeRecord = activeResult?.records[activeRecordIndex] || null

  const getSourceIcon = (type: string) => {
    switch (type) {
      case "file":
        return <FileText className="h-4 w-4 text-blue-500" />
      case "url":
        return <LinkIcon className="h-4 w-4 text-green-500" />
      case "text":
        return <Type className="h-4 w-4 text-purple-500" />
      default:
        return null
    }
  }

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "pending":
        return <Badge variant="outline">待处理</Badge>
      case "processing":
        return (
          <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200">
            <Loader2 className="h-3 w-3 mr-1 animate-spin" />
            处理中
          </Badge>
        )
      case "success":
        return (
          <Badge variant="outline" className="bg-green-50 text-green-700 border-green-200">
            <CheckCircle className="h-3 w-3 mr-1" />
            成功
          </Badge>
        )
      case "error":
        return (
          <Badge variant="outline" className="bg-red-50 text-red-700 border-red-200">
            <AlertCircle className="h-3 w-3 mr-1" />
            失败
          </Badge>
        )
      default:
        return null
    }
  }

  // 检查是否所有数据源都已处理完成
  const allProcessed = results.every((r) => r.status !== "pending" && r.status !== "processing")
  // 检查是否有成功提取的记录
  const hasSuccessRecords = results.some((r) => r.status === "success" && r.records.length > 0)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium">AI 提取预览</h3>
        <Button variant="outline" size="sm" onClick={handleRetryAll} disabled={isExtracting}>
          <RefreshCw className="mr-2 h-4 w-4" />
          重新提取全部
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div className="md:col-span-1 space-y-4">
          <h4 className="text-sm font-medium">数据源</h4>
          <div className="space-y-2 max-h-[500px] overflow-y-auto pr-2">
            {results.map((result) => {
              const source = dataSources.find((s) => s.id === result.sourceId)
              if (!source) return null

              return (
                <Card
                  key={result.sourceId}
                  className={`cursor-pointer ${activeSourceId === result.sourceId ? "border-purple-500" : ""}`}
                  onClick={() => {
                    setActiveSourceId(result.sourceId)
                    setActiveRecordIndex(0)
                    if (result.records.length > 0) {
                      setEditedRecord(result.records[0])
                    }
                  }}
                >
                  <CardContent className="p-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center gap-2">
                        {getSourceIcon(source.type)}
                        <div className="truncate max-w-[150px]">{source.name}</div>
                      </div>
                      <div className="flex items-center gap-2">
                        {getStatusBadge(result.status)}
                        {result.status === "success" && <Badge variant="outline">{result.records.length} 条记录</Badge>}
                        {result.status === "error" && (
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-6 w-6 p-0"
                            onClick={(e) => {
                              e.stopPropagation()
                              handleRetryExtraction(result.sourceId)
                            }}
                          >
                            <RefreshCw className="h-3 w-3" />
                          </Button>
                        )}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              )
            })}
          </div>
        </div>

        <div className="md:col-span-2 space-y-4">
          {activeSource && activeResult ? (
            <>
              <div className="flex items-center justify-between">
                <h4 className="text-sm font-medium">
                  {activeSource.name} {activeResult.status === "success" && `(${activeResult.records.length} 条记录)`}
                </h4>
                {activeResult.status === "success" && activeResult.records.length > 1 && (
                  <Select
                    value={activeRecordIndex.toString()}
                    onValueChange={(value) => {
                      const index = Number.parseInt(value)
                      setActiveRecordIndex(index)
                      setEditedRecord(activeResult.records[index])
                    }}
                  >
                    <SelectTrigger className="w-[180px]">
                      <SelectValue placeholder="选择记录" />
                    </SelectTrigger>
                    <SelectContent>
                      {activeResult.records.map((_, index) => (
                        <SelectItem key={index} value={index.toString()}>
                          记录 #{index + 1}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              </div>

              {activeResult.status === "processing" && (
                <div className="flex flex-col items-center justify-center p-12">
                  <Loader2 className="h-8 w-8 animate-spin text-purple-500 mb-4" />
                  <p className="text-muted-foreground">AI 正在提取数据，请稍候...</p>
                </div>
              )}

              {activeResult.status === "pending" && (
                <div className="flex flex-col items-center justify-center p-12">
                  <Button onClick={startExtraction} disabled={isExtracting}>
                    {isExtracting ? (
                      <>
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                        处理中...
                      </>
                    ) : (
                      <>
                        <RefreshCw className="mr-2 h-4 w-4" />
                        开始提取
                      </>
                    )}
                  </Button>
                </div>
              )}

              {activeResult.status === "error" && (
                <div className="flex flex-col items-center justify-center p-12 text-center">
                  <AlertCircle className="h-8 w-8 text-red-500 mb-4" />
                  <p className="text-muted-foreground mb-4">{activeResult.error}</p>
                  <Button onClick={() => handleRetryExtraction(activeResult.sourceId)}>
                    <RefreshCw className="mr-2 h-4 w-4" />
                    重试
                  </Button>
                </div>
              )}

              {activeResult.status === "success" && activeRecord && (
                <Tabs defaultValue="form" className="w-full">
                  <TabsList className="grid w-full grid-cols-2">
                    <TabsTrigger value="form">表单视图</TabsTrigger>
                    <TabsTrigger value="json">JSON 视图</TabsTrigger>
                  </TabsList>
                  <TabsContent value="form" className="space-y-4 pt-4">
                    {schema.fields.map((field) => (
                      <div key={field.id} className="space-y-2">
                        <Label htmlFor={`field-${field.id}`}>
                          {field.displayName}
                          {field.required && <span className="text-red-500 ml-1">*</span>}
                        </Label>
                        {field.type === "text" ? (
                          <Textarea
                            id={`field-${field.id}`}
                            value={activeRecord[field.name] || ""}
                            onChange={(e) => handleRecordChange(field.name, e.target.value)}
                            rows={4}
                          />
                        ) : field.type === "array" ? (
                          <div className="space-y-2">
                            <pre className="text-xs bg-muted p-2 rounded-md overflow-auto">
                              {JSON.stringify(activeRecord[field.name] || [], null, 2)}
                            </pre>
                            <p className="text-xs text-muted-foreground">数组类型字段，请在 JSON 视图中编辑</p>
                          </div>
                        ) : (
                          <Input
                            id={`field-${field.id}`}
                            value={activeRecord[field.name] || ""}
                            onChange={(e) => handleRecordChange(field.name, e.target.value)}
                            type={field.type === "number" ? "number" : "text"}
                          />
                        )}
                      </div>
                    ))}
                  </TabsContent>
                  <TabsContent value="json" className="pt-4">
                    <Card>
                      <CardHeader>
                        <CardTitle>JSON 数据</CardTitle>
                      </CardHeader>
                      <CardContent>
                        <pre className="text-xs bg-muted p-4 rounded-md overflow-auto max-h-[400px]">
                          {JSON.stringify(activeRecord, null, 2)}
                        </pre>
                      </CardContent>
                    </Card>
                  </TabsContent>
                </Tabs>
              )}
            </>
          ) : (
            <div className="flex flex-col items-center justify-center p-12 text-center">
              <p className="text-muted-foreground">请选择一个数据源查看提取结果</p>
            </div>
          )}
        </div>
      </div>

      <Button onClick={handleComplete} disabled={isExtracting || !hasSuccessRecords} className="w-full">
        确认提取结果
      </Button>
    </div>
  )
}
