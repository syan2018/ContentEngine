"use client"

import { useState, type ChangeEvent, type FormEvent } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Checkbox } from "@/components/ui/checkbox"
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Save, RotateCcw } from "lucide-react"
import type { Schema, SchemaField } from "@/lib/types" // 假设类型定义文件

interface ManualSingleEntryFormProps {
  schema: Schema
}

export default function ManualSingleEntryForm({ schema }: ManualSingleEntryFormProps) {
  const initialFormState = () => {
    const state: Record<string, any> = {}
    schema.fields.forEach((field) => {
      if (field.type === "boolean") {
        state[field.name] = false
      } else if (field.type === "array" || field.type === "object") {
        state[field.name] = "" // 初始为空字符串，用户可以输入JSON或逗号分隔列表
      } else {
        state[field.name] = ""
      }
    })
    return state
  }

  const [formData, setFormData] = useState<Record<string, any>>(initialFormState())
  const [isSaving, setIsSaving] = useState(false)
  const [saveStatus, setSaveStatus] = useState<"success" | "error" | null>(null)

  const handleChange = (e: ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value, type } = e.target
    if (type === "checkbox" && e.target instanceof HTMLInputElement) {
      setFormData((prev) => ({ ...prev, [name]: e.target.checked }))
    } else {
      setFormData((prev) => ({ ...prev, [name]: value }))
    }
  }

  const handleSelectChange = (fieldName: string, value: string) => {
    setFormData((prev) => ({ ...prev, [fieldName]: value }))
  }

  const handleCheckboxChange = (fieldName: string, checked: boolean) => {
    setFormData((prev) => ({ ...prev, [fieldName]: checked }))
  }

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setIsSaving(true)
    setSaveStatus(null)

    // 准备要保存的数据，对特殊类型进行转换
    const dataToSave: Record<string, any> = {}
    for (const field of schema.fields) {
      let value = formData[field.name]
      if (field.type === "number") {
        value = Number.parseFloat(value)
        if (isNaN(value)) value = null // 或者处理错误
      } else if (field.type === "array") {
        try {
          // 尝试解析为JSON数组，如果失败，则按逗号分隔
          value = JSON.parse(value)
        } catch (err) {
          value =
            typeof value === "string"
              ? value
                  .split(",")
                  .map((item) => item.trim())
                  .filter((item) => item)
              : []
        }
      } else if (field.type === "object") {
        try {
          value = JSON.parse(value)
        } catch (err) {
          // 如果JSON解析失败，可以设置错误状态或保留为字符串让后端处理
          console.error(`Error parsing JSON for field ${field.name}:`, err)
          // For now, let's keep it as string if parsing fails, or set to null
          // value = null; // Or handle error appropriately
        }
      }
      dataToSave[field.name] = value
    }

    console.log("Saving data:", dataToSave)
    // 模拟保存操作
    await new Promise((resolve) => setTimeout(resolve, 1500))

    // 模拟成功或失败
    const success = Math.random() > 0.2
    if (success) {
      setSaveStatus("success")
      setFormData(initialFormState()) // 清空表单
    } else {
      setSaveStatus("error")
    }
    setIsSaving(false)

    setTimeout(() => setSaveStatus(null), 3000)
  }

  const renderField = (field: SchemaField) => {
    const commonProps = {
      id: field.name,
      name: field.name,
      required: field.required,
      onChange: handleChange,
      className: "mt-1 block w-full",
    }

    return (
      <div key={field.id} className="space-y-1">
        <Label htmlFor={field.name}>
          {field.displayName}
          {field.required && <span className="text-red-500 ml-1">*</span>}
        </Label>
        {field.description && <p className="text-xs text-muted-foreground">{field.description}</p>}

        {field.type === "string" && <Input type="text" {...commonProps} value={formData[field.name] || ""} />}
        {field.type === "text" && <Textarea {...commonProps} value={formData[field.name] || ""} rows={3} />}
        {field.type === "number" && <Input type="number" {...commonProps} value={formData[field.name] || ""} />}
        {field.type === "date" && <Input type="date" {...commonProps} value={formData[field.name] || ""} />}
        {field.type === "boolean" && (
          <div className="flex items-center space-x-2 pt-2">
            <Checkbox
              id={field.name}
              name={field.name}
              checked={formData[field.name] || false}
              onCheckedChange={(checked) => handleCheckboxChange(field.name, checked as boolean)}
            />
            <Label htmlFor={field.name} className="font-normal">
              是/否
            </Label>
          </div>
        )}
        {field.type === "enum" && field.options && (
          <Select
            name={field.name}
            required={field.required}
            value={formData[field.name] || ""}
            onValueChange={(value) => handleSelectChange(field.name, value)}
          >
            <SelectTrigger id={field.name}>
              <SelectValue placeholder={`选择 ${field.displayName}`} />
            </SelectTrigger>
            <SelectContent>
              {field.options.map((option) => (
                <SelectItem key={option} value={option}>
                  {option}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        )}
        {field.type === "array" && (
          <Input
            type="text"
            {...commonProps}
            value={formData[field.name] || ""}
            placeholder={'项目1, 项目2 或 ["项目1", "项目2"]'}
          />
        )}
        {field.type === "object" && (
          <Textarea
            {...commonProps}
            value={formData[field.name] || ""}
            rows={3}
            placeholder={'例如: {"key1": "value1", "key2": 123}'}
          />
        )}
      </div>
    )
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>录入新记录</CardTitle>
      </CardHeader>
      <form onSubmit={handleSubmit}>
        <CardContent className="space-y-4">{schema.fields.map((field) => renderField(field))}</CardContent>
        <CardFooter className="flex justify-between">
          <div>
            {saveStatus === "success" && <p className="text-green-600">记录保存成功！</p>}
            {saveStatus === "error" && <p className="text-red-600">保存失败，请重试。</p>}
          </div>
          <div className="flex gap-2">
            <Button type="button" variant="outline" onClick={() => setFormData(initialFormState())} disabled={isSaving}>
              <RotateCcw className="mr-2 h-4 w-4" />
              重置
            </Button>
            <Button type="submit" disabled={isSaving}>
              {isSaving ? (
                <>
                  <Save className="mr-2 h-4 w-4 animate-spin" />
                  保存中...
                </>
              ) : (
                <>
                  <Save className="mr-2 h-4 w-4" />
                  保存记录
                </>
              )}
            </Button>
          </div>
        </CardFooter>
      </form>
    </Card>
  )
}
