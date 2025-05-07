"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Label } from "@/components/ui/label"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Sparkles, Loader2 } from "lucide-react"

export default function AIAssistedSchemaCreation() {
  const [description, setDescription] = useState("")
  const [sampleData, setSampleData] = useState("")
  const [isGenerating, setIsGenerating] = useState(false)
  const [generatedSchema, setGeneratedSchema] = useState<null | any>(null)

  const generateSchema = () => {
    setIsGenerating(true)
    // 模拟 AI 处理
    setTimeout(() => {
      setGeneratedSchema({
        name: "游戏角色",
        key: "game_character",
        description: "定义游戏中的角色属性和特征",
        fields: [
          { id: 1, name: "name", displayName: "角色名称", type: "string", required: true },
          { id: 2, name: "race", displayName: "种族", type: "string", required: true },
          { id: 3, name: "class", displayName: "职业", type: "string", required: true },
          { id: 4, name: "level", displayName: "等级", type: "number", required: true },
          { id: 5, name: "health", displayName: "生命值", type: "number", required: true },
          { id: 6, name: "skills", displayName: "技能列表", type: "array", required: false },
          { id: 7, name: "background", displayName: "背景故事", type: "text", required: false },
        ],
      })
      setIsGenerating(false)
    }, 2000)
  }

  return (
    <div className="space-y-6">
      <div className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="ai-description">描述您需要的数据结构</Label>
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
            <span className="text-sm text-muted-foreground ml-2">提供一段样例数据可以帮助 AI 更准确地理解您的需求</span>
          </Label>
          <Textarea
            id="sample-data"
            placeholder="粘贴一段包含相关信息的文本或表格数据..."
            rows={6}
            value={sampleData}
            onChange={(e) => setSampleData(e.target.value)}
          />
        </div>
        <Button onClick={generateSchema} disabled={!description || isGenerating} className="w-full">
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

      {generatedSchema && (
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>AI 生成结果</CardTitle>
              <CardDescription>AI 根据您的描述生成了以下数据结构，您可以进行修改和调整</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="grid gap-4 md:grid-cols-2">
                <div className="space-y-2">
                  <Label>数据结构名称</Label>
                  <Input value={generatedSchema.name} />
                </div>
                <div className="space-y-2">
                  <Label>唯一标识符</Label>
                  <Input value={generatedSchema.key} />
                </div>
              </div>
              <div className="space-y-2">
                <Label>描述</Label>
                <Textarea value={generatedSchema.description} rows={2} />
              </div>
              <div className="space-y-2">
                <Label>生成的字段</Label>
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
                      {generatedSchema.fields.map((field: any) => (
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
              </div>
            </CardContent>
            <CardFooter className="flex justify-end space-x-4">
              <Button variant="outline">编辑字段</Button>
              <Button>保存数据结构</Button>
            </CardFooter>
          </Card>
        </div>
      )}
    </div>
  )
}
