"use client"

import { useState } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  Database,
  Edit,
  FileInput,
  Search,
  Sparkles,
  Plus,
  Trash2,
  Save,
  X,
  Check,
  Copy,
  Download,
  BarChart3,
} from "lucide-react"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Switch } from "@/components/ui/switch"
import Link from "next/link"
import type { Schema, SchemaField } from "@/lib/types"

// 模拟获取Schema详情的函数
const getMockSchemaDetail = (
  schemaId: string,
): Schema & {
  createdAt: string
  updatedAt: string
  recordCount: number
  lastDataEntry: string
} => {
  const schemas = {
    "1": {
      id: "1",
      name: "游戏角色",
      key: "game_character",
      description: "定义游戏中的角色属性和特征，包含基本信息、能力值和背景故事等",
      fields: [
        {
          id: "f1",
          name: "character_name",
          displayName: "角色名称",
          type: "string" as const,
          required: true,
          description: "角色的唯一名称",
        },
        {
          id: "f2",
          name: "race",
          displayName: "种族",
          type: "enum" as const,
          required: true,
          options: ["人类", "精灵", "矮人", "兽人", "暗精灵"],
          description: "角色的种族",
        },
        {
          id: "f3",
          name: "class",
          displayName: "职业",
          type: "string" as const,
          required: true,
          description: "角色的主要职业",
        },
        {
          id: "f4",
          name: "level",
          displayName: "等级",
          type: "number" as const,
          required: true,
          description: "角色的当前等级",
        },
        {
          id: "f5",
          name: "health_points",
          displayName: "生命值",
          type: "number" as const,
          required: true,
          description: "角色的当前生命值",
        },
        {
          id: "f6",
          name: "is_active",
          displayName: "是否活跃",
          type: "boolean" as const,
          required: false,
          description: "角色当前是否活跃",
        },
        {
          id: "f7",
          name: "bio",
          displayName: "角色简介",
          type: "text" as const,
          required: false,
          description: "角色的背景故事或简介",
        },
        {
          id: "f8",
          name: "inventory_items",
          displayName: "物品栏",
          type: "array" as const,
          required: false,
          description: "角色携带的物品列表",
        },
        {
          id: "f9",
          name: "attributes",
          displayName: "属性",
          type: "object" as const,
          required: false,
          description: "角色的核心属性 (JSON格式)",
        },
        {
          id: "f10",
          name: "last_seen_date",
          displayName: "最后出现日期",
          type: "date" as const,
          required: false,
          description: "角色最后一次出现的日期",
        },
      ],
      createdAt: "2023-04-15 10:30:22",
      updatedAt: "2023-05-02 16:45:37",
      recordCount: 48,
      lastDataEntry: "2023-05-01 14:20:15",
    },
    "2": {
      id: "2",
      name: "游戏道具",
      key: "game_item",
      description: "定义游戏中的各类道具及其属性",
      fields: [
        {
          id: "f1",
          name: "item_name",
          displayName: "道具名称",
          type: "string" as const,
          required: true,
          description: "道具的名称",
        },
        {
          id: "f2",
          name: "type",
          displayName: "道具类型",
          type: "enum" as const,
          required: true,
          options: ["武器", "防具", "消耗品", "材料", "任务物品"],
          description: "道具的分类",
        },
        {
          id: "f3",
          name: "rarity",
          displayName: "稀有度",
          type: "enum" as const,
          required: true,
          options: ["普通", "精良", "稀有", "史诗", "传说"],
          description: "道具的稀有程度",
        },
        {
          id: "f4",
          name: "value",
          displayName: "价值",
          type: "number" as const,
          required: true,
          description: "道具的金币价值",
        },
        {
          id: "f5",
          name: "description",
          displayName: "描述",
          type: "text" as const,
          required: false,
          description: "道具的详细描述",
        },
        {
          id: "f6",
          name: "effects",
          displayName: "效果",
          type: "array" as const,
          required: false,
          description: "道具的特殊效果列表",
        },
        {
          id: "f7",
          name: "is_tradeable",
          displayName: "可交易",
          type: "boolean" as const,
          required: false,
          description: "道具是否可以交易",
        },
        {
          id: "f8",
          name: "created_date",
          displayName: "创建日期",
          type: "date" as const,
          required: false,
          description: "道具在游戏中的创建日期",
        },
      ],
      createdAt: "2023-04-20 09:15:30",
      updatedAt: "2023-04-28 11:22:45",
      recordCount: 156,
      lastDataEntry: "2023-04-27 16:30:00",
    },
  }

  return schemas[schemaId as keyof typeof schemas] || schemas["1"]
}

interface SchemaDetailViewProps {
  schemaId: string
}

export default function SchemaDetailView({ schemaId }: SchemaDetailViewProps) {
  const [schemaData, setSchemaData] = useState(() => getMockSchemaDetail(schemaId))
  const [isEditing, setIsEditing] = useState(false)
  const [editingField, setEditingField] = useState<string | null>(null)
  const [isSaving, setSaving] = useState(false)
  const [activeTab, setActiveTab] = useState("overview")

  // 编辑Schema基本信息
  const [editedSchema, setEditedSchema] = useState({
    name: schemaData.name,
    description: schemaData.description,
  })

  // 编辑字段信息
  const [editedField, setEditedField] = useState<SchemaField | null>(null)

  const handleSaveSchema = async () => {
    setSaving(true)
    // 模拟保存操作
    await new Promise((resolve) => setTimeout(resolve, 1000))

    setSchemaData((prev) => ({
      ...prev,
      ...editedSchema,
      updatedAt: new Date().toLocaleString("zh-CN"),
    }))

    setIsEditing(false)
    setSaving(false)
  }

  const handleSaveField = async () => {
    if (!editedField) return

    setSaving(true)
    await new Promise((resolve) => setTimeout(resolve, 800))

    setSchemaData((prev) => ({
      ...prev,
      fields: prev.fields.map((field) => (field.id === editedField.id ? editedField : field)),
      updatedAt: new Date().toLocaleString("zh-CN"),
    }))

    setEditingField(null)
    setEditedField(null)
    setSaving(false)
  }

  const handleDeleteField = async (fieldId: string) => {
    if (!confirm("确定要删除这个字段吗？此操作不可撤销。")) return

    setSaving(true)
    await new Promise((resolve) => setTimeout(resolve, 500))

    setSchemaData((prev) => ({
      ...prev,
      fields: prev.fields.filter((field) => field.id !== fieldId),
      updatedAt: new Date().toLocaleString("zh-CN"),
    }))

    setSaving(false)
  }

  const startEditField = (field: SchemaField) => {
    setEditedField({ ...field })
    setEditingField(field.id)
  }

  const cancelEditField = () => {
    setEditingField(null)
    setEditedField(null)
  }

  const getFieldTypeIcon = (type: string) => {
    const icons = {
      string: "Aa",
      text: "¶",
      number: "#",
      boolean: "✓",
      date: "📅",
      enum: "⚏",
      array: "[]",
      object: "{}",
    }
    return icons[type as keyof typeof icons] || "?"
  }

  const getFieldTypeBadgeColor = (type: string) => {
    const colors = {
      string: "bg-blue-100 text-blue-800",
      text: "bg-purple-100 text-purple-800",
      number: "bg-green-100 text-green-800",
      boolean: "bg-yellow-100 text-yellow-800",
      date: "bg-pink-100 text-pink-800",
      enum: "bg-indigo-100 text-indigo-800",
      array: "bg-orange-100 text-orange-800",
      object: "bg-gray-100 text-gray-800",
    }
    return colors[type as keyof typeof colors] || "bg-gray-100 text-gray-800"
  }

  return (
    <div className="space-y-6">
      {/* Schema基本信息 */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex-1">
              {isEditing ? (
                <div className="space-y-4">
                  <div className="space-y-2">
                    <Label htmlFor="schema-name">Schema名称</Label>
                    <Input
                      id="schema-name"
                      value={editedSchema.name}
                      onChange={(e) => setEditedSchema((prev) => ({ ...prev, name: e.target.value }))}
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor="schema-description">描述</Label>
                    <Textarea
                      id="schema-description"
                      value={editedSchema.description}
                      onChange={(e) => setEditedSchema((prev) => ({ ...prev, description: e.target.value }))}
                      rows={3}
                    />
                  </div>
                </div>
              ) : (
                <div>
                  <CardTitle className="text-2xl">{schemaData.name}</CardTitle>
                  <CardDescription className="mt-2">{schemaData.description}</CardDescription>
                </div>
              )}
            </div>
            <div className="flex items-center gap-2">
              <Badge variant="outline">{schemaData.fields.length} 个字段</Badge>
              <Badge variant="outline">{schemaData.recordCount} 条记录</Badge>
              {isEditing ? (
                <div className="flex gap-2">
                  <Button variant="outline" size="sm" onClick={() => setIsEditing(false)} disabled={isSaving}>
                    <X className="mr-2 h-4 w-4" />
                    取消
                  </Button>
                  <Button size="sm" onClick={handleSaveSchema} disabled={isSaving}>
                    {isSaving ? (
                      <>
                        <Save className="mr-2 h-4 w-4 animate-spin" />
                        保存中...
                      </>
                    ) : (
                      <>
                        <Save className="mr-2 h-4 w-4" />
                        保存
                      </>
                    )}
                  </Button>
                </div>
              ) : (
                <Button variant="outline" size="sm" onClick={() => setIsEditing(true)}>
                  <Edit className="mr-2 h-4 w-4" />
                  编辑
                </Button>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">Schema标识</p>
              <p className="font-mono text-sm">{schemaData.key}</p>
            </div>
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">创建时间</p>
              <p className="font-medium">{schemaData.createdAt}</p>
            </div>
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">最后更新</p>
              <p className="font-medium">{schemaData.updatedAt}</p>
            </div>
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">最后录入</p>
              <p className="font-medium">{schemaData.lastDataEntry}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* 快捷操作 */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center">
            <Sparkles className="h-5 w-5 mr-2 text-purple-500" />
            快捷操作
          </CardTitle>
          <CardDescription>对此Schema进行常用操作</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <Link href={`/data-entry/manual/${schemaId}`} className="block">
              <Button variant="outline" className="w-full h-20 flex flex-col items-center justify-center">
                <FileInput className="h-6 w-6 mb-2 text-blue-500" />
                <span>手动录入数据</span>
              </Button>
            </Link>
            <Link href={`/data-entry/ai/${schemaId}`} className="block">
              <Button variant="outline" className="w-full h-20 flex flex-col items-center justify-center">
                <Sparkles className="h-6 w-6 mb-2 text-purple-500" />
                <span>AI辅助录入</span>
              </Button>
            </Link>
            <Link href={`/data-explorer/${schemaId}`} className="block">
              <Button variant="outline" className="w-full h-20 flex flex-col items-center justify-center">
                <Search className="h-6 w-6 mb-2 text-green-500" />
                <span>浏览数据</span>
              </Button>
            </Link>
            <Button variant="outline" className="w-full h-20 flex flex-col items-center justify-center">
              <BarChart3 className="h-6 w-6 mb-2 text-orange-500" />
              <span>数据统计</span>
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* 详细信息标签页 */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="overview">字段概览</TabsTrigger>
          <TabsTrigger value="fields">字段管理</TabsTrigger>
          <TabsTrigger value="usage">使用情况</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-4 pt-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Database className="h-5 w-5 mr-2 text-blue-500" />
                字段结构概览
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="rounded-md border">
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead className="w-[50px]">类型</TableHead>
                      <TableHead>字段名</TableHead>
                      <TableHead>显示名</TableHead>
                      <TableHead>必填</TableHead>
                      <TableHead>描述</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {schemaData.fields.map((field) => (
                      <TableRow key={field.id}>
                        <TableCell>
                          <Badge className={`text-xs ${getFieldTypeBadgeColor(field.type)}`}>
                            {getFieldTypeIcon(field.type)}
                          </Badge>
                        </TableCell>
                        <TableCell className="font-mono text-sm">{field.name}</TableCell>
                        <TableCell className="font-medium">{field.displayName}</TableCell>
                        <TableCell>
                          {field.required ? (
                            <Badge variant="destructive" className="text-xs">
                              必填
                            </Badge>
                          ) : (
                            <Badge variant="outline" className="text-xs">
                              可选
                            </Badge>
                          )}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground max-w-xs truncate">
                          {field.description}
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="fields" className="space-y-4 pt-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-medium">字段管理</h3>
            <Button>
              <Plus className="mr-2 h-4 w-4" />
              添加字段
            </Button>
          </div>

          <div className="space-y-4">
            {schemaData.fields.map((field) => (
              <Card key={field.id}>
                <CardHeader className="py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      <Badge className={`${getFieldTypeBadgeColor(field.type)}`}>
                        {getFieldTypeIcon(field.type)} {field.type}
                      </Badge>
                      <div>
                        <h4 className="font-medium">{field.displayName}</h4>
                        <p className="text-sm text-muted-foreground font-mono">{field.name}</p>
                      </div>
                      {field.required && (
                        <Badge variant="destructive" className="text-xs">
                          必填
                        </Badge>
                      )}
                    </div>
                    <div className="flex items-center gap-2">
                      {editingField === field.id ? (
                        <div className="flex gap-2">
                          <Button variant="outline" size="sm" onClick={cancelEditField} disabled={isSaving}>
                            <X className="h-4 w-4" />
                          </Button>
                          <Button size="sm" onClick={handleSaveField} disabled={isSaving}>
                            {isSaving ? <Save className="h-4 w-4 animate-spin" /> : <Check className="h-4 w-4" />}
                          </Button>
                        </div>
                      ) : (
                        <div className="flex gap-2">
                          <Button variant="ghost" size="sm" onClick={() => startEditField(field)}>
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            onClick={() => handleDeleteField(field.id)}
                            className="text-destructive hover:text-destructive"
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      )}
                    </div>
                  </div>
                </CardHeader>
                <CardContent className="py-2">
                  {editingField === field.id && editedField ? (
                    <div className="space-y-4">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <Label htmlFor={`field-name-${field.id}`}>字段名</Label>
                          <Input
                            id={`field-name-${field.id}`}
                            value={editedField.name}
                            onChange={(e) =>
                              setEditedField((prev) => (prev ? { ...prev, name: e.target.value } : null))
                            }
                          />
                        </div>
                        <div className="space-y-2">
                          <Label htmlFor={`field-display-${field.id}`}>显示名</Label>
                          <Input
                            id={`field-display-${field.id}`}
                            value={editedField.displayName}
                            onChange={(e) =>
                              setEditedField((prev) => (prev ? { ...prev, displayName: e.target.value } : null))
                            }
                          />
                        </div>
                      </div>
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                          <Label htmlFor={`field-type-${field.id}`}>数据类型</Label>
                          <Select
                            value={editedField.type}
                            onValueChange={(value) =>
                              setEditedField((prev) => (prev ? { ...prev, type: value as any } : null))
                            }
                          >
                            <SelectTrigger id={`field-type-${field.id}`}>
                              <SelectValue />
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
                            checked={editedField.required}
                            onCheckedChange={(checked) =>
                              setEditedField((prev) => (prev ? { ...prev, required: checked } : null))
                            }
                          />
                          <Label htmlFor={`field-required-${field.id}`}>必填字段</Label>
                        </div>
                      </div>
                      {editedField.type === "enum" && (
                        <div className="space-y-2">
                          <Label htmlFor={`field-options-${field.id}`}>选项值（用逗号分隔）</Label>
                          <Input
                            id={`field-options-${field.id}`}
                            value={editedField.options?.join(", ") || ""}
                            onChange={(e) =>
                              setEditedField((prev) =>
                                prev
                                  ? {
                                      ...prev,
                                      options: e.target.value
                                        .split(",")
                                        .map((o) => o.trim())
                                        .filter((o) => o),
                                    }
                                  : null,
                              )
                            }
                          />
                        </div>
                      )}
                      <div className="space-y-2">
                        <Label htmlFor={`field-desc-${field.id}`}>字段描述</Label>
                        <Textarea
                          id={`field-desc-${field.id}`}
                          value={editedField.description || ""}
                          onChange={(e) =>
                            setEditedField((prev) => (prev ? { ...prev, description: e.target.value } : null))
                          }
                          rows={2}
                        />
                      </div>
                    </div>
                  ) : (
                    <div className="space-y-2">
                      <p className="text-sm">{field.description}</p>
                      {field.type === "enum" && field.options && (
                        <div className="flex flex-wrap gap-1">
                          {field.options.map((option) => (
                            <Badge key={option} variant="outline" className="text-xs">
                              {option}
                            </Badge>
                          ))}
                        </div>
                      )}
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        </TabsContent>

        <TabsContent value="usage" className="space-y-4 pt-4">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Card>
              <CardHeader>
                <CardTitle>数据统计</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">总记录数:</span>
                  <span className="font-medium">{schemaData.recordCount}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">字段数量:</span>
                  <span className="font-medium">{schemaData.fields.length}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">必填字段:</span>
                  <span className="font-medium">{schemaData.fields.filter((f) => f.required).length}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">最后录入:</span>
                  <span className="font-medium">{schemaData.lastDataEntry}</span>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>相关操作</CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <Link href={`/data-entry/manual/${schemaId}`}>
                  <Button variant="outline" className="w-full justify-start">
                    <FileInput className="mr-2 h-4 w-4" />
                    手动录入数据
                  </Button>
                </Link>
                <Link href={`/data-entry/ai/${schemaId}`}>
                  <Button variant="outline" className="w-full justify-start">
                    <Sparkles className="mr-2 h-4 w-4" />
                    AI辅助录入
                  </Button>
                </Link>
                <Link href={`/data-explorer/${schemaId}`}>
                  <Button variant="outline" className="w-full justify-start">
                    <Search className="mr-2 h-4 w-4" />
                    浏览和查询数据
                  </Button>
                </Link>
                <Button variant="outline" className="w-full justify-start">
                  <Download className="mr-2 h-4 w-4" />
                  导出Schema定义
                </Button>
                <Button variant="outline" className="w-full justify-start">
                  <Copy className="mr-2 h-4 w-4" />
                  复制Schema
                </Button>
              </CardContent>
            </Card>
          </div>

          <Card>
            <CardHeader>
              <CardTitle>字段类型分布</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {Object.entries(
                  schemaData.fields.reduce(
                    (acc, field) => {
                      acc[field.type] = (acc[field.type] || 0) + 1
                      return acc
                    },
                    {} as Record<string, number>,
                  ),
                ).map(([type, count]) => (
                  <div key={type} className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                      <Badge className={`text-xs ${getFieldTypeBadgeColor(type)}`}>
                        {getFieldTypeIcon(type)} {type}
                      </Badge>
                    </div>
                    <div className="flex items-center gap-2">
                      <div className="w-20 bg-muted rounded-full h-2">
                        <div
                          className="bg-primary h-2 rounded-full"
                          style={{ width: `${(count / schemaData.fields.length) * 100}%` }}
                        />
                      </div>
                      <span className="text-sm font-medium w-8 text-right">{count}</span>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
