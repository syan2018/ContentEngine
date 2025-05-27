"use client"

import { useState, useEffect } from "react"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Button } from "@/components/ui/button"
import { Sparkles, Copy, Check, AlertCircle } from "lucide-react"
import type { ReasoningTransactionDefinition } from "../reasoning-transaction-creation-wizard"

interface PromptTemplateStepProps {
  transactionDefinition: ReasoningTransactionDefinition
  updateTransactionDefinition: (updates: Partial<ReasoningTransactionDefinition>) => void
}

export default function PromptTemplateStep({
  transactionDefinition,
  updateTransactionDefinition,
}: PromptTemplateStepProps) {
  const [templateContent, setTemplateContent] = useState(transactionDefinition.promptTemplate.templateContent)
  const [copied, setCopied] = useState(false)
  const [activeTab, setActiveTab] = useState("editor")
  const [previewContent, setPreviewContent] = useState("")
  const [detectedViewNames, setDetectedViewNames] = useState<string[]>([])
  const [missingViews, setMissingViews] = useState<string[]>([])

  // 获取所有可用的视图名称
  const availableViews = transactionDefinition.queryDefinitions.map((query) => query.outputViewName)

  // 当模板内容变化时，更新检测到的视图名称
  useEffect(() => {
    const regex = /\{\{([^.}]+)\./g
    const matches = [...templateContent.matchAll(regex)]
    const viewNames = [...new Set(matches.map((match) => match[1].trim()))]

    // Only update state if the detected view names have actually changed
    if (!arraysEqual(viewNames, detectedViewNames)) {
      setDetectedViewNames(viewNames)

      // Check for missing views
      const missing = viewNames.filter((view) => !availableViews.includes(view))
      setMissingViews(missing)
    }

    // Helper function to compare arrays
    function arraysEqual(a: string[], b: string[]) {
      if (a.length !== b.length) return false
      return a.every((val, idx) => val === b[idx])
    }

    // Don't update the transaction definition here to avoid the loop
  }, [templateContent, availableViews, detectedViewNames])

  // Add a separate useEffect to update the transaction definition
  useEffect(() => {
    // This will only run when detectedViewNames changes
    if (detectedViewNames.length > 0) {
      updateTransactionDefinition({
        promptTemplate: {
          ...transactionDefinition.promptTemplate,
          expectedInputViewNames: detectedViewNames,
        },
      })
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [detectedViewNames])

  // 当用户切换到预览标签时，生成预览内容
  useEffect(() => {
    if (activeTab === "preview") {
      generatePreview()
    }
  }, [activeTab])

  // 保存模板内容
  const saveTemplateContent = () => {
    updateTransactionDefinition({
      promptTemplate: {
        ...transactionDefinition.promptTemplate,
        templateContent,
        // Don't update expectedInputViewNames here, it's handled by the useEffect
      },
    })
  }

  // 生成预览内容
  const generatePreview = () => {
    // 在实际应用中，这里会使用真实的模板引擎和示例数据
    // 这里只是简单替换占位符以进行演示
    let preview = templateContent

    // 为每个检测到的视图创建模拟数据
    detectedViewNames.forEach((viewName) => {
      const viewData = generateMockDataForView(viewName)

      // 替换所有 {{ViewName.field}} 格式的占位符
      const regex = new RegExp(`\\{\\{${viewName}\\.([^}]+)\\}\\}`, "g")
      preview = preview.replace(regex, (match, field) => {
        return viewData[field] || `[未找到字段: ${field}]`
      })
    })

    setPreviewContent(preview)
  }

  // 生成视图的模拟数据
  const generateMockDataForView = (viewName: string) => {
    // 根据视图名称生成不同的模拟数据
    if (viewName.toLowerCase().includes("character") || viewName.toLowerCase().includes("npc")) {
      return {
        name: "艾莉娅",
        race: "精灵",
        class: "法师",
        level: "25",
        health: "850",
        mana: "1200",
        background: "艾莉娅出生于精灵王国的一个古老家族，从小展现出非凡的魔法天赋。",
      }
    } else if (viewName.toLowerCase().includes("scene") || viewName.toLowerCase().includes("scenario")) {
      return {
        name: "魔法森林",
        description: "一片充满神秘能量的古老森林，高大的树木遮蔽了阳光，地面覆盖着荧光苔藓。",
        environment: "森林",
        mood: "神秘",
        time_of_day: "黄昏",
        weather: "多雾",
      }
    } else if (viewName.toLowerCase().includes("quest") || viewName.toLowerCase().includes("task")) {
      return {
        title: "寻找失落的魔法卷轴",
        description: "传说中的远古卷轴被藏在魔法森林深处的遗迹中，找到它并带回来。",
        difficulty: "高",
        reward_type: "魔法物品",
        reward_amount: "1",
      }
    } else {
      // 默认数据
      return {
        id: "12345",
        name: "示例名称",
        description: "这是一个示例描述",
        status: "活跃",
      }
    }
  }

  // 复制视图字段引用
  const copyFieldReference = (viewName: string, fieldName: string) => {
    const reference = `{{${viewName}.${fieldName}}}`
    navigator.clipboard.writeText(reference)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  // 获取视图的字段列表
  const getViewFields = (viewName: string) => {
    const query = transactionDefinition.queryDefinitions.find((q) => q.outputViewName === viewName)
    if (!query) return []

    // 在实际应用中，这里应该根据 query.sourceSchemaName 和 query.selectFields 获取真实的字段列表
    // 这里简化处理，直接返回 selectFields
    return query.selectFields
  }

  return (
    <div className="space-y-6">
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="editor">模板编辑器</TabsTrigger>
          <TabsTrigger value="preview">预览</TabsTrigger>
        </TabsList>
        <TabsContent value="editor" className="space-y-4 pt-4">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <Label htmlFor="prompt-template">Prompt 模板</Label>
              {missingViews.length > 0 && (
                <div className="flex items-center text-amber-500 text-sm">
                  <AlertCircle className="h-4 w-4 mr-1" />
                  <span>模板中包含未定义的视图</span>
                </div>
              )}
            </div>
            <Textarea
              id="prompt-template"
              placeholder="输入 Prompt 模板，使用 {{ViewName.field}} 引用数据字段..."
              rows={12}
              value={templateContent}
              onChange={(e) => setTemplateContent(e.target.value)}
              className="font-mono text-sm"
            />
            <p className="text-xs text-muted-foreground">
              使用 {"{{ViewName.field}}"} 格式引用数据视图中的字段，例如 {"{{CharacterView.name}}"}
            </p>
          </div>

          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-2">
              <span className="text-sm font-medium">检测到的视图:</span>
              <div className="flex flex-wrap gap-1">
                {detectedViewNames.map((viewName) => (
                  <Badge
                    key={viewName}
                    variant={availableViews.includes(viewName) ? "default" : "destructive"}
                    className="px-2 py-0"
                  >
                    {viewName}
                  </Badge>
                ))}
              </div>
            </div>
            <Button onClick={saveTemplateContent} size="sm">
              保存模板
            </Button>
          </div>

          {missingViews.length > 0 && (
            <div className="p-3 border rounded-md bg-amber-50 text-amber-800">
              <p className="text-sm">
                警告: 模板中引用了未定义的视图:{" "}
                {missingViews.map((view, i) => (
                  <span key={view}>
                    <Badge variant="outline" className="font-mono bg-amber-100">
                      {view}
                    </Badge>
                    {i < missingViews.length - 1 ? ", " : ""}
                  </span>
                ))}
              </p>
              <p className="text-xs mt-1">请确保在数据查询步骤中定义了这些视图，或修改模板中的视图名称。</p>
            </div>
          )}
        </TabsContent>
        <TabsContent value="preview" className="space-y-4 pt-4">
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Prompt 预览</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="whitespace-pre-wrap bg-muted p-4 rounded-md text-sm">
                {previewContent || "模板为空或未包含任何视图引用"}
              </div>
            </CardContent>
          </Card>
          <Button onClick={generatePreview} className="w-full">
            <Sparkles className="mr-2 h-4 w-4" />
            刷新预览
          </Button>
        </TabsContent>
      </Tabs>

      <div className="space-y-4 pt-4 border-t">
        <h3 className="text-lg font-medium">可用数据字段</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {availableViews.map((viewName) => {
            const fields = getViewFields(viewName)
            return (
              <Card key={viewName}>
                <CardHeader className="py-3">
                  <CardTitle className="text-base">{viewName}</CardTitle>
                </CardHeader>
                <CardContent className="py-2">
                  <div className="space-y-2">
                    {fields.map((field) => (
                      <div key={field} className="flex items-center justify-between">
                        <code className="text-xs bg-muted px-2 py-1 rounded">{field}</code>
                        <Button
                          variant="ghost"
                          size="sm"
                          className="h-7 px-2"
                          onClick={() => copyFieldReference(viewName, field)}
                        >
                          {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                        </Button>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )
          })}
        </div>
      </div>
    </div>
  )
}
