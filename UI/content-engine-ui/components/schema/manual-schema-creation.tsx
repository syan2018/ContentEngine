"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Switch } from "@/components/ui/switch"
import { Plus, Trash2 } from "lucide-react"

export default function ManualSchemaCreation() {
  const [fields, setFields] = useState([
    { id: 1, name: "", displayName: "", type: "", required: false, description: "" },
  ])

  const addField = () => {
    const newId = fields.length > 0 ? Math.max(...fields.map((f) => f.id)) + 1 : 1
    setFields([...fields, { id: newId, name: "", displayName: "", type: "", required: false, description: "" }])
  }

  const removeField = (id: number) => {
    setFields(fields.filter((field) => field.id !== id))
  }

  const updateField = (id: number, key: string, value: any) => {
    setFields(fields.map((field) => (field.id === id ? { ...field, [key]: value } : field)))
  }

  return (
    <div className="space-y-6">
      <div className="space-y-4">
        <div className="grid gap-4 md:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="schema-name">数据结构名称</Label>
            <Input id="schema-name" placeholder="例如：游戏角色" />
          </div>
          <div className="space-y-2">
            <Label htmlFor="schema-key">唯一标识符</Label>
            <Input id="schema-key" placeholder="例如：game_character" />
          </div>
        </div>
        <div className="space-y-2">
          <Label htmlFor="schema-description">描述</Label>
          <Textarea id="schema-description" placeholder="描述这个数据结构的用途和包含的信息..." rows={3} />
        </div>
      </div>

      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-medium">字段定义</h3>
          <Button onClick={addField} variant="outline" size="sm">
            <Plus className="mr-1 h-4 w-4" />
            添加字段
          </Button>
        </div>

        {fields.map((field) => (
          <Card key={field.id}>
            <CardHeader className="py-4">
              <div className="flex items-center justify-between">
                <CardTitle className="text-base">字段 #{field.id}</CardTitle>
                <Button variant="ghost" size="sm" onClick={() => removeField(field.id)} className="h-8 w-8 p-0">
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
      </div>

      <div className="flex justify-end space-x-4">
        <Button variant="outline">取消</Button>
        <Button>保存数据结构</Button>
      </div>
    </div>
  )
}
