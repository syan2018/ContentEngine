"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent } from "@/components/ui/card"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Sparkles, FileText, LinkIcon, Type } from "lucide-react"
import type { DataSource } from "./ai-data-entry-wizard"
import { Switch } from "@/components/ui/switch"

interface MappingConfigStepProps {
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
  extractionMode: "one-to-one" | "batch"
  onComplete: (mappings: Record<string, string>) => void
}

export default function MappingConfigStep({ schema, dataSources, extractionMode, onComplete }: MappingConfigStepProps) {
  const [mappingMode, setMappingMode] = useState<"auto" | "manual">("auto")
  const [fieldMappings, setFieldMappings] = useState<Record<string, string>>({})
  const [customInstructions, setCustomInstructions] = useState("")
  const [selectedSource, setSelectedSource] = useState<string | null>(dataSources.length > 0 ? dataSources[0].id : null)

  const handleMappingModeChange = (checked: boolean) => {
    setMappingMode(checked ? "manual" : "auto")
  }

  const handleFieldMappingChange = (fieldName: string, mapping: string) => {
    setFieldMappings({
      ...fieldMappings,
      [fieldName]: mapping,
    })
  }

  const handleComplete = () => {
    // 如果是自动映射模式，我们使用自定义指令作为映射
    if (mappingMode === "auto") {
      onComplete({
        _autoMapping: customInstructions || "自动提取所有字段",
      })
    } else {
      onComplete(fieldMappings)
    }
  }

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

  const selectedSourceData = dataSources.find((source) => source.id === selectedSource)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium">字段映射配置</h3>
        <div className="flex items-center space-x-2">
          <Label htmlFor="mapping-mode">手动映射</Label>
          <Switch id="mapping-mode" checked={mappingMode === "manual"} onCheckedChange={handleMappingModeChange} />
        </div>
      </div>

      {mappingMode === "auto" ? (
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="custom-instructions">自定义提取指令 (可选)</Label>
            <Textarea
              id="custom-instructions"
              placeholder={
                extractionMode === "one-to-one"
                  ? "例如：从文档中提取角色的名称、种族、职业和等级信息。如果找不到确切的等级，请根据描述估计一个合理的值。"
                  : "例如：从文档中提取所有角色信息，每个角色应包含名称、种族、职业和等级。如果找到多个角色，请分别提取。"
              }
              rows={5}
              value={customInstructions}
              onChange={(e) => setCustomInstructions(e.target.value)}
            />
          </div>

          <Card>
            <CardContent className="p-4">
              <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Sparkles className="h-4 w-4" />
                <p>
                  AI 将自动分析数据源内容，并尝试提取与{" "}
                  <span className="font-medium text-foreground">{schema.name}</span> 相关的所有字段。
                  {extractionMode === "batch" && " 如果数据源包含多条记录，AI 将尝试提取所有记录。"}
                </p>
              </div>
            </CardContent>
          </Card>
        </div>
      ) : (
        <div className="space-y-4">
          {dataSources.length > 1 && (
            <div className="space-y-2">
              <Label htmlFor="source-selector">选择数据源预览</Label>
              <Select value={selectedSource || ""} onValueChange={setSelectedSource}>
                <SelectTrigger id="source-selector">
                  <SelectValue placeholder="选择数据源" />
                </SelectTrigger>
                <SelectContent>
                  {dataSources.map((source) => (
                    <SelectItem key={source.id} value={source.id}>
                      <div className="flex items-center gap-2">
                        {getSourceIcon(source.type)}
                        {source.name}
                      </div>
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}

          {selectedSourceData && (
            <Card className="mb-4">
              <CardContent className="p-4 max-h-40 overflow-auto">
                <pre className="text-xs whitespace-pre-wrap">
                  {selectedSourceData.type === "url"
                    ? selectedSourceData.url
                    : selectedSourceData.content.substring(0, 500) +
                      (selectedSourceData.content.length > 500 ? "..." : "")}
                </pre>
              </CardContent>
            </Card>
          )}

          <div className="space-y-4">
            {schema.fields.map((field) => (
              <div key={field.id} className="space-y-2">
                <Label htmlFor={`field-mapping-${field.id}`}>
                  {field.displayName}
                  {field.required && <span className="text-red-500 ml-1">*</span>}
                </Label>
                <Input
                  id={`field-mapping-${field.id}`}
                  placeholder={`提取 ${field.displayName} 的指令或路径`}
                  value={fieldMappings[field.name] || ""}
                  onChange={(e) => handleFieldMappingChange(field.name, e.target.value)}
                />
                <p className="text-xs text-muted-foreground">
                  {field.type === "string" && "文本字段，例如：'提取角色名称' 或 '$.name'"}
                  {field.type === "number" && "数字字段，例如：'提取角色等级' 或 '$.level'"}
                  {field.type === "array" && "数组字段，例如：'提取所有技能名称' 或 '$.skills[*].name'"}
                  {field.type === "text" && "长文本字段，例如：'提取角色背景故事' 或 '$.background'"}
                </p>
              </div>
            ))}
          </div>
        </div>
      )}

      <Button onClick={handleComplete} className="w-full">
        <Sparkles className="mr-2 h-4 w-4" />
        {mappingMode === "auto" ? "使用 AI 自动映射" : "应用映射规则"}
      </Button>
    </div>
  )
}
