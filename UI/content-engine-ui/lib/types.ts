// 定义 Schema 字段类型
export type SchemaField = {
  id: string
  name: string // 内部字段名，例如 user_name
  displayName: string // UI 显示名，例如 用户名
  type: "string" | "text" | "number" | "boolean" | "date" | "enum" | "array" | "object"
  required: boolean
  description?: string
  options?: string[] // 用于 enum 类型
  // 可以添加更多元数据，如校验规则、默认值等
}

// 定义 Schema 类型
export type Schema = {
  id: string
  name: string // Schema 的 UI 显示名称，例如 游戏角色
  key: string // Schema 的唯一标识符，例如 game_character
  description: string
  fields: SchemaField[]
}
