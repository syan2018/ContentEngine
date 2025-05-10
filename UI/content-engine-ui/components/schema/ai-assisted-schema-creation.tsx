"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Sparkles, Loader2, ArrowRight, Save, Plus, Trash2 } from "lucide-react"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import { Badge } from "@/components/ui/badge"
import { Separator } from "@/components/ui/separator"
import { Steps, Step } from "@/components/ui/steps"

// 定义字段类型
type Field = {
  id: number
  name: string
  displayName: string
  type: string
  required: boolean
  description: string
  options?: string[] // 用于枚举类型
}

// 定义 Schema 类型
type Schema = {
  name: string
  key: string
  description: string
  fields: Field[]
}

export default function AIAssistedSchemaCreation() {
  // 状态管理
  const [currentStep, setCurrentStep] = useState(0)
  const [description, setDescription] = useState("")
  const [sampleData, setSampleData] = useState("")
  const [refinementPrompt, setRefinementPrompt] = useState("")
  const [isGenerating, setIsGenerating] = useState(false)
  const [isRefining, setIsRefining] = useState(false)
  const [generatedSchema, setGeneratedSchema] = useState<Schema | null>(null)
  const [activeTab, setActiveTab] = useState("fields")
  const [nextFieldId, setNextFieldId] = useState(1)

  // 生成初始 Schema
  const generateSchema = () => {
    if (!description.trim()) return

    setIsGenerating(true)
    // 模拟 AI 处理
    setTimeout(() => {
      // 示例生成结果 - 在实际应用中，这会是从 AI 服务获取的结果
      const schema: Schema = {
        name: "游戏角色",
        key: "game_character",
        description: "定义游戏中的角色属性和特征",
        fields: [
          { id: 1, name: "name", displayName: "角色名称", type: "string", required: true, description: "角色的名字" },
          {
            id: 2,
            name: "race",
            displayName: "种族",
            type: "enum",
            required: true,
            description: "角色的种族",
            options: ["人类", "精灵", "矮人", "兽人", "暗精灵"],
          },
          { id: 3, name: "class", displayName: "职业", type: "string", required: true, description: "角色的职业类型" },
          { id: 4, name: "level", displayName: "等级", type: "number", required: true, description: "角色的当前等级" },
          {
            id: 5,
            name: "health",
            displayName: "生命值",
            type: "number",
            required: true,
            description: "角色的最大生命值",
          },
          {
            id: 6,
            name: "skills",
            displayName: "技能列表",
            type: "array",
            required: false,
            description: "角色掌握的技能列表",
          },
          {
            id: 7,
            name: "background",
            displayName: "背景故事",
            type: "text",
            required: false,
            description: "角色的背景故事描述",
          },
        ],
      }

      setGeneratedSchema(schema)
      setNextFieldId(schema.fields.length + 1)
      setIsGenerating(false)
      setCurrentStep(1)
    }, 2000)
  }

  // 基于自然语言指令精炼 Schema
  const refineSchema = () => {
    if (!refinementPrompt.trim() || !generatedSchema) return

    setIsRefining(true)
    // 模拟 AI 处理
    setTimeout(() => {
      // 根据指令修改 Schema - 在实际应用中，这会是从 AI 服务获取的结果
      const refinedSchema = { ...generatedSchema }

      // 示例：如果指令包含"添加魔法值"，则添加一个魔法值字段
      if (
        refinementPrompt.toLowerCase().includes("添加魔法值") ||
        refinementPrompt.toLowerCase().includes("增加魔法值")
      ) {
        refinedSchema.fields.push({
          id: nextFieldId,
          name: "mana",
          displayName: "魔法值",
          type: "number",
          required: true,
          description: "角色的最大魔法值",
        })
        setNextFieldId(nextFieldId + 1)
      }

      // 示例：如果指令包含"添加装备栏位"，则添加一个装备字段
      if (refinementPrompt.toLowerCase().includes("添加装备") || refinementPrompt.toLowerCase().includes("增加装备")) {
        refinedSchema.fields.push({
          id: nextFieldId,
          name: "equipment",
          displayName: "装备",
          type: "object",
          required: false,
          description: "角色当前装备的物品",
        })
        setNextFieldId(nextFieldId + 1)
      }

      // 示例：如果指令包含"修改种族为下拉选择"，则修改种族字段类型
      if (refinementPrompt.toLowerCase().includes("种族") && refinementPrompt.toLowerCase().includes("选择")) {
        const raceField = refinedSchema.fields.find((f) => f.name === "race")
        if (raceField) {
          raceField.type = "enum"
          raceField.options = ["人类", "精灵", "矮人", "兽人", "暗精灵", "龙人", "侏儒"]
        }
      }

      setGeneratedSchema(refinedSchema)
      setIsRefining(false)
      setRefinementPrompt("")
    }, 1500)
  }

  // 添加新字段
  const addField = () => {
    if (!generatedSchema) return

    const newField: Field = {
      id: nextFieldId,
      name: "",
      displayName: "",
      type: "",
      required: false,
      description: "",
    }

    setGeneratedSchema({
      ...generatedSchema,
      fields: [...generatedSchema.fields, newField],
    })

    setNextFieldId(nextFieldId + 1)
  }

  // 更新字段
  const updateField = (id: number, key: string, value: any) => {
    if (!generatedSchema) return

    setGeneratedSchema({
      ...generatedSchema,
      fields: generatedSchema.fields.map((field) => (field.id === id ? { ...field, [key]: value } : field)),
    })
  }

  // 删除字段
  const removeField = (id: number) => {
    if (!generatedSchema) return

    setGeneratedSchema({
      ...generatedSchema,
      fields: generatedSchema.fields.filter((field) => field.id !== id),
    })
  }

  // 更新 Schema 基本信息
  const updateSchema = (key: string, value: string) => {
    if (!generatedSchema) return

    setGeneratedSchema({
      ...generatedSchema,
      [key]: value,
    })
  }

  return (
    <div className="space-y-6">
      <Steps currentStep={currentStep} className="mb-8">
        <Step title="初始生成" description="描述您需要的数据结构" />
        <Step title="精炼与编辑" description="完善数据结构定义" />
        <Step title="完成" description="保存并使用" />
      </Steps>

      {currentStep === 0 && (
        <div className="space-y-4">
          <div className="space-y-2">
            <Label htmlFor="ai-description">
              描述您需要的数据结构
              <span className="text-sm text-muted-foreground ml-2">尽可能详细地描述您需要的数据结构和字段</span>
            </Label>
            <Textarea
              id="ai-description"
              placeholder="例如：我需要一个游戏角色的数据结构，包含姓名、种族、职业、等级、生命值、技能列表和背景故事描述"
              rows={4}
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="sample-data">
              样例数据（可选）
              <span className="text-sm text-muted-foreground ml-2">
                提供一段样例数据可以帮助 AI 更准确地理解您的需求
              </span>
            </Label>
            <Textarea
              id="sample-data"
              placeholder="粘贴一段包含相关信息的文本或表格数据..."
              rows={6}
              value={sampleData}
              onChange={(e) => setSampleData(e.target.value)}
            />
          </div>
          <Button onClick={generateSchema} disabled={!description.trim() || isGenerating} className="w-full">
            {isGenerating ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                正在生成...
              </>
            ) : (
              <>
                <Sparkles className="mr-2 h-4 w-4" />
                AI 智能生成数据结构
              </>
            )}
          </Button>
        </div>
      )}

      {currentStep === 1 && generatedSchema && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>AI 辅助精炼</CardTitle>
              <CardDescription>使用自然语言指令进一步完善数据结构</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <Label htmlFor="refinement-prompt">
                  修改指令
                  <span className="text-sm text-muted-foreground ml-2">
                    告诉 AI 您想如何修改数据结构，例如：添加新字段、修改字段类型等
                  </span>
                </Label>
                <Textarea
                  id="refinement-prompt"
                  placeholder="例如：添加一个魔法值字段，将种族改为下拉选择，添加装备栏位..."
                  rows={3}
                  value={refinementPrompt}
                  onChange={(e) => setRefinementPrompt(e.target.value)}
                />
              </div>
              <Button onClick={refineSchema} disabled={!refinementPrompt.trim() || isRefining} className="w-full">
                {isRefining ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    正在处理...
                  </>
                ) : (
                  <>
                    <Sparkles className="mr-2 h-4 w-4" />
                    AI 辅助修改
                  </>
                )}
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>数据结构定义</CardTitle>
              <CardDescription>您可以直接编辑以下内容或使用 AI 辅助修改</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="schema-name">数据结构名称</Label>
                  <Input
                    id="schema-name"
                    value={generatedSchema.name}
                    onChange={(e) => updateSchema("name", e.target.value)}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="schema-key">唯一标识符</Label>
                  <Input
                    id="schema-key"
                    value={generatedSchema.key}
                    onChange={(e) => updateSchema("key", e.target.value)}
                  />
                </div>
              </div>
              <div className="space-y-2">
                <Label htmlFor="schema-description">描述</Label>
                <Textarea
                  id="schema-description"
                  rows={2}
                  value={generatedSchema.description}
                  onChange={(e) => updateSchema("description", e.target.value)}
                />
              </div>

              <Separator />

              <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
                <TabsList className="grid w-full grid-cols-2">
                  <TabsTrigger value="fields">字段列表</TabsTrigger>
                  <TabsTrigger value="json">JSON 视图</TabsTrigger>
                </TabsList>
                <TabsContent value="fields" className="space-y-4 pt-4">
                  <div className="flex items-center justify-between">
                    <h3 className="text-lg font-medium">字段定义</h3>
                    <Button onClick={addField} variant="outline" size="sm">
                      <Plus className="mr-1 h-4 w-4" />
                      添加字段
                    </Button>
                  </div>

                  {generatedSchema.fields.map((field) => (
                    <Card key={field.id}>
                      <CardHeader className="py-4">
                        <div className="flex items-center justify-between">
                          <CardTitle className="text-base">
                            {field.displayName || "新字段"}
                            {field.required && <Badge className="ml-2">必填</Badge>}
                          </CardTitle>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => removeField(field.id)}
                            className="h-8 w-8 p-0"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </CardHeader>
                      <CardContent className="py-2 space-y-4">
                        <div className="grid gap-4 md:grid-cols-2">
                          <div className="space-y-2">
                            <Label htmlFor={`field-name-${field.id}`}>字段名</Label>
                            <Input
                              id={`field-name-${field.id}`}
                              placeholder="例如：name"
                              value={field.name}
                              onChange={(e) => updateField(field.id, "name", e.target.value)}
                            />
                          </div>
                          <div className="space-y-2">
                            <Label htmlFor={`field-display-${field.id}`}>显示名</Label>
                            <Input
                              id={`field-display-${field.id}`}
                              placeholder="例如：角色名称"
                              value={field.displayName}
                              onChange={(e) => updateField(field.id, "displayName", e.target.value)}
                            />
                          </div>
                        </div>
                        <div className="grid gap-4 md:grid-cols-2">
                          <div className="space-y-2">
                            <Label htmlFor={`field-type-${field.id}`}>数据类型</Label>
                            <Select value={field.type} onValueChange={(value) => updateField(field.id, "type", value)}>
                              <SelectTrigger id={`field-type-${field.id}`}>
                                <SelectValue placeholder="选择数据类型" />
                              </SelectTrigger>
                              <SelectContent>
                                <SelectItem value="string">文本</SelectItem>
                                <SelectItem value="text">长文本</SelectItem>
                                <SelectItem value="number">数字</SelectItem>
                                <SelectItem value="boolean">布尔值</SelectItem>
                                <SelectItem value="date">日期</SelectItem>
                                <SelectItem value="enum">枚举/选项</SelectItem>
                                <SelectItem value="array">数组/列表</SelectItem>
                                <SelectItem value="object">嵌套对象</SelectItem>
                              </SelectContent>
                            </Select>
                          </div>
                          <div className="flex items-center space-x-2 pt-8">
                            <Switch
                              id={`field-required-${field.id}`}
                              checked={field.required}
                              onCheckedChange={(checked) => updateField(field.id, "required", checked)}
                            />
                            <Label htmlFor={`field-required-${field.id}`}>必填字段</Label>
                          </div>
                        </div>

                        {field.type === "enum" && (
                          <div className="space-y-2">
                            <Label htmlFor={`field-options-${field.id}`}>选项值（用逗号分隔）</Label>
                            <Input
                              id={`field-options-${field.id}`}
                              placeholder="例如：选项1,选项2,选项3"
                              value={field.options?.join(", ") || ""}
                              onChange={(e) =>
                                updateField(
                                  field.id,
                                  "options",
                                  e.target.value.split(",").map((o) => o.trim()),
                                )
                              }
                            />
                          </div>
                        )}

                        <div className="space-y-2">
                          <Label htmlFor={`field-desc-${field.id}`}>字段描述</Label>
                          <Textarea
                            id={`field-desc-${field.id}`}
                            placeholder="描述这个字段的用途和格式要求..."
                            rows={2}
                            value={field.description}
                            onChange={(e) => updateField(field.id, "description", e.target.value)}
                          />
                        </div>
                      </CardContent>
                    </Card>
                  ))}
                </TabsContent>
                <TabsContent value="json" className="pt-4">
                  <Card>
                    <CardHeader>
                      <CardTitle>JSON 结构</CardTitle>
                      <CardDescription>数据结构的 JSON 表示</CardDescription>
                    </CardHeader>
                    <CardContent>
                      <pre className="bg-muted p-4 rounded-md overflow-auto max-h-[400px]">
                        {JSON.stringify(generatedSchema, null, 2)}
                      </pre>
                    </CardContent>
                  </Card>
                </TabsContent>
              </Tabs>
            </CardContent>
            <CardFooter className="flex justify-between">
              <Button variant="outline" onClick={() => setCurrentStep(0)}>
                返回
              </Button>
              <Button onClick={() => setCurrentStep(2)}>
                <ArrowRight className="mr-2 h-4 w-4" />
                下一步
              </Button>
            </CardFooter>
          </Card>
        </div>
      )}

      {currentStep === 2 && generatedSchema && (
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle>确认并保存</CardTitle>
              <CardDescription>检查您的数据结构并保存</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="space-y-2">
                <h3 className="text-lg font-medium">{generatedSchema.name}</h3>
                <p className="text-muted-foreground">{generatedSchema.description}</p>
              </div>

              <div className="border rounded-md">
                <table className="w-full">
                  <thead>
                    <tr className="border-b">
                      <th className="px-4 py-2 text-left font-medium">字段名</th>
                      <th className="px-4 py-2 text-left font-medium">显示名</th>
                      <th className="px-4 py-2 text-left font-medium">类型</th>
                      <th className="px-4 py-2 text-left font-medium">必填</th>
                    </tr>
                  </thead>
                  <tbody>
                    {generatedSchema.fields.map((field) => (
                      <tr key={field.id} className="border-b">
                        <td className="px-4 py-2">{field.name}</td>
                        <td className="px-4 py-2">{field.displayName}</td>
                        <td className="px-4 py-2">{field.type}</td>
                        <td className="px-4 py-2">{field.required ? "是" : "否"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </CardContent>
            <CardFooter className="flex justify-between">
              <Button variant="outline" onClick={() => setCurrentStep(1)}>
                返回编辑
              </Button>
              <Button>
                <Save className="mr-2 h-4 w-4" />
                保存数据结构
              </Button>
            </CardFooter>
          </Card>
        </div>
      )}
    </div>
  )
}
