"use client"

import { useState, useCallback } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Checkbox } from "@/components/ui/checkbox"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { PlusCircle, Trash2, SaveAll, Upload } from "lucide-react"
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
  DialogClose,
} from "@/components/ui/dialog"
import type { Schema, SchemaField } from "@/lib/types" // 假设类型定义文件

interface ManualTableEntryProps {
  schema: Schema
}

type TableRowData = Record<string, any>

export default function ManualTableEntry({ schema }: ManualTableEntryProps) {
  const createEmptyRow = useCallback((): TableRowData => {
    const row: TableRowData = { _id: Date.now() + Math.random().toString(36).substring(2, 9) } // Unique temp ID
    schema.fields.forEach((field) => {
      row[field.name] = field.type === "boolean" ? false : ""
    })
    return row
  }, [schema.fields])

  const [rows, setRows] = useState<TableRowData[]>([createEmptyRow()])
  const [pasteData, setPasteData] = useState("")
  const [isPasteModalOpen, setIsPasteModalOpen] = useState(false)
  const [columnMapping, setColumnMapping] = useState<Record<number, string | "ignore">>({})
  const [parsedPastedData, setParsedPastedData] = useState<string[][]>([])
  const [isMappingModalOpen, setIsMappingModalOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [saveStatus, setSaveStatus] = useState<"success" | "error" | null>(null)

  const handleCellChange = (rowIndex: number, fieldName: string, value: any, fieldType: string) => {
    const newRows = [...rows]
    if (fieldType === "boolean") {
      newRows[rowIndex][fieldName] = value as boolean
    } else if (fieldType === "number") {
      newRows[rowIndex][fieldName] = value === "" ? "" : Number(value)
    } else {
      newRows[rowIndex][fieldName] = value
    }
    setRows(newRows)
  }

  const addRow = () => {
    setRows([...rows, createEmptyRow()])
  }

  const removeRow = (rowIndex: number) => {
    const newRows = rows.filter((_, index) => index !== rowIndex)
    setRows(newRows)
  }

  const handlePasteDataParse = () => {
    const lines = pasteData.trim().split("\n")
    const parsed = lines.map((line) => line.split("\t")) // Assuming tab-separated for Excel paste
    setParsedPastedData(parsed)

    // Auto-map if headers match or same number of columns
    const headers = parsed[0]
    const newColumnMapping: Record<number, string | "ignore"> = {}
    if (headers) {
      headers.forEach((header, index) => {
        const matchedField = schema.fields.find(
          (f) => f.displayName.toLowerCase() === header.toLowerCase() || f.name.toLowerCase() === header.toLowerCase(),
        )
        if (matchedField) {
          newColumnMapping[index] = matchedField.name
        } else {
          newColumnMapping[index] = "ignore"
        }
      })
    } else if (parsed.length > 0 && parsed[0].length === schema.fields.length) {
      // If no headers but column count matches, map sequentially
      schema.fields.forEach((field, index) => {
        if (index < parsed[0].length) {
          newColumnMapping[index] = field.name
        }
      })
    }

    setColumnMapping(newColumnMapping)
    setIsPasteModalOpen(false)
    setIsMappingModalOpen(true)
  }

  const applyColumnMapping = () => {
    const dataToImport = parsedPastedData.slice(isHeaderRow(parsedPastedData[0]) ? 1 : 0) // Skip header row if detected
    const newRowsData: TableRowData[] = dataToImport.map((pastedRow) => {
      const newRow = createEmptyRow()
      Object.entries(columnMapping).forEach(([colIndexStr, fieldName]) => {
        const colIndex = Number.parseInt(colIndexStr, 10)
        if (fieldName !== "ignore" && fieldName && pastedRow[colIndex] !== undefined) {
          const field = schema.fields.find((f) => f.name === fieldName)
          if (field) {
            let value = pastedRow[colIndex]
            if (field.type === "boolean") value = ["true", "yes", "1"].includes(value.toLowerCase())
            else if (field.type === "number") value = Number.parseFloat(value) || ""
            // TODO: Add parsing for array and object if needed from paste
            newRow[fieldName] = value
          }
        }
      })
      return newRow
    })
    setRows((prevRows) => [
      ...prevRows.filter((r) =>
        Object.values(r)
          .slice(1)
          .some((val) => val !== "" && val !== false),
      ),
      ...newRowsData,
    ]) // Keep existing non-empty rows
    setIsMappingModalOpen(false)
    setPasteData("")
    setParsedPastedData([])
  }

  const isHeaderRow = (row: string[]): boolean => {
    if (!row) return false
    // Simple heuristic: if many cells match schema display names or names, it's likely a header
    let matchCount = 0
    row.forEach((cell) => {
      if (
        schema.fields.some(
          (f) => f.displayName.toLowerCase() === cell.toLowerCase() || f.name.toLowerCase() === cell.toLowerCase(),
        )
      ) {
        matchCount++
      }
    })
    return matchCount / row.length > 0.5 // More than 50% cells match
  }

  const handleSaveAll = async () => {
    setIsSaving(true)
    setSaveStatus(null)
    const validRows = rows.filter((row) =>
      schema.fields.every((field) => {
        if (field.required) {
          const value = row[field.name]
          return (
            value !== "" &&
            value !== undefined &&
            value !== null &&
            (field.type === "boolean" || String(value).trim() !== "")
          )
        }
        return true
      }),
    )

    if (validRows.length !== rows.length) {
      alert("部分行未填写必填项，或数据无效，这些行将不会被保存。")
    }
    if (validRows.length === 0) {
      setIsSaving(false)
      setSaveStatus("error") // Or a specific message like "no_valid_rows"
      setTimeout(() => setSaveStatus(null), 3000)
      return
    }

    // Prepare data for saving (similar to single entry)
    const dataToSave = validRows.map((row) => {
      const record: Record<string, any> = {}
      schema.fields.forEach((field) => {
        let value = row[field.name]
        if (field.type === "number") value = Number.parseFloat(value)
        else if (field.type === "array")
          value =
            typeof value === "string"
              ? value
                  .split(",")
                  .map((item) => item.trim())
                  .filter((item) => item)
              : []
        else if (field.type === "object") {
          try {
            value = JSON.parse(value)
          } catch {
            /* handle error or keep as string */
          }
        }
        record[field.name] = value
      })
      return record
    })

    console.log("Saving all records:", dataToSave)
    await new Promise((resolve) => setTimeout(resolve, 2000)) // Simulate API call

    const success = Math.random() > 0.2
    if (success) {
      setSaveStatus("success")
      setRows([createEmptyRow()]) // Reset table
    } else {
      setSaveStatus("error")
    }
    setIsSaving(false)
    setTimeout(() => setSaveStatus(null), 3000)
  }

  const renderCellInput = (rowIndex: number, field: SchemaField) => {
    const value = rows[rowIndex][field.name]

    if (field.type === "boolean") {
      return (
        <Checkbox
          checked={value || false}
          onCheckedChange={(checked) => handleCellChange(rowIndex, field.name, checked, field.type)}
        />
      )
    }
    if (field.type === "enum" && field.options) {
      return (
        <Select
          value={value || ""}
          onValueChange={(selectValue) => handleCellChange(rowIndex, field.name, selectValue, field.type)}
        >
          <SelectTrigger className="h-8 text-xs">
            <SelectValue placeholder="选择" />
          </SelectTrigger>
          <SelectContent>
            {field.options.map((option) => (
              <SelectItem key={option} value={option} className="text-xs">
                {option}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      )
    }
    if (field.type === "date") {
      return (
        <Input
          type="date"
          value={value || ""}
          onChange={(e) => handleCellChange(rowIndex, field.name, e.target.value, field.type)}
          className="h-8 text-xs p-1"
        />
      )
    }
    // For string, number, text, array, object - use Input for simplicity in table
    return (
      <Input
        type={field.type === "number" ? "number" : "text"}
        value={value || ""}
        onChange={(e) => handleCellChange(rowIndex, field.name, e.target.value, field.type)}
        placeholder={field.type === "array" ? "项1,项2" : field.type === "object" ? "{}" : ""}
        className="h-8 text-xs p-1"
      />
    )
  }

  return (
    <Card>
      <CardHeader>
        <div className="flex justify-between items-center">
          <CardTitle>批量录入数据</CardTitle>
          <Dialog open={isPasteModalOpen} onOpenChange={setIsPasteModalOpen}>
            <DialogTrigger asChild>
              <Button variant="outline">
                <Upload className="mr-2 h-4 w-4" /> 从表格粘贴数据
              </Button>
            </DialogTrigger>
            <DialogContent className="sm:max-w-md">
              <DialogHeader>
                <DialogTitle>粘贴表格数据</DialogTitle>
                <DialogDescription>
                  从 Excel, Google Sheets 或 CSV 文件中复制数据并粘贴到下方文本框。数据应为制表符或逗号分隔。
                </DialogDescription>
              </DialogHeader>
              <Textarea
                placeholder="在此处粘贴数据..."
                value={pasteData}
                onChange={(e) => setPasteData(e.target.value)}
                rows={10}
              />
              <DialogFooter>
                <Button type="button" onClick={handlePasteDataParse} disabled={!pasteData.trim()}>
                  解析数据
                </Button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
        <CardDescription>通过表格界面快速录入多条记录，或从外部表格粘贴数据。</CardDescription>
      </CardHeader>
      <CardContent>
        {rows.length === 0 && (
          <div className="text-center py-8 text-muted-foreground">
            <p>表格中暂无数据。</p>
            <p>您可以点击“添加行”手动输入，或使用“从表格粘贴数据”功能批量导入。</p>
          </div>
        )}
        {rows.length > 0 && (
          <div className="overflow-x-auto">
            <Table className="min-w-full">
              <TableHeader>
                <TableRow>
                  {schema.fields.map((field) => (
                    <TableHead key={field.id} className="text-xs px-2 py-1 whitespace-nowrap">
                      {field.displayName}
                      {field.required && <span className="text-red-500 ml-1">*</span>}
                    </TableHead>
                  ))}
                  <TableHead className="w-[50px] text-xs px-2 py-1">操作</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rows.map((row, rowIndex) => (
                  <TableRow key={row._id}>
                    {schema.fields.map((field) => (
                      <TableCell key={field.id} className="px-2 py-1 align-top">
                        {renderCellInput(rowIndex, field)}
                      </TableCell>
                    ))}
                    <TableCell className="px-2 py-1 align-top">
                      <Button variant="ghost" size="icon" onClick={() => removeRow(rowIndex)} className="h-8 w-8">
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )}
      </CardContent>
      <CardFooter className="flex flex-col items-stretch gap-4">
        <Button onClick={addRow} variant="outline" className="w-full">
          <PlusCircle className="mr-2 h-4 w-4" /> 添加行
        </Button>
        <div className="flex justify-between items-center">
          <div>
            {saveStatus === "success" && <p className="text-green-600">所有有效记录已保存！</p>}
            {saveStatus === "error" && <p className="text-red-600">保存失败或无有效数据，请检查。</p>}
          </div>
          <Button onClick={handleSaveAll} disabled={isSaving || rows.length === 0} className="w-auto">
            {isSaving ? (
              <>
                <SaveAll className="mr-2 h-4 w-4 animate-spin" /> 保存中...
              </>
            ) : (
              <>
                <SaveAll className="mr-2 h-4 w-4" /> 保存所有记录 ({rows.length})
              </>
            )}
          </Button>
        </div>
      </CardFooter>

      {/* Column Mapping Modal */}
      <Dialog open={isMappingModalOpen} onOpenChange={setIsMappingModalOpen}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>列映射</DialogTitle>
            <DialogDescription>请将粘贴数据的列映射到目标数据结构的字段。系统已尝试自动匹配。</DialogDescription>
          </DialogHeader>
          {parsedPastedData.length > 0 && parsedPastedData[0] && (
            <div className="space-y-4 max-h-[60vh] overflow-y-auto p-1">
              {parsedPastedData[0].map((header, colIndex) => (
                <div key={colIndex} className="grid grid-cols-2 gap-4 items-center">
                  <div className="space-y-1">
                    <Label className="text-xs">
                      源列 {colIndex + 1}: <span className="font-semibold truncate">{header}</span>
                    </Label>
                    <Input
                      readOnly
                      value={
                        parsedPastedData
                          .slice(1, 4)
                          .map((r) => r[colIndex] || "")
                          .join(" | ") || "无数据预览"
                      }
                      className="text-xs h-7 bg-muted"
                      title="数据预览 (最多3行)"
                    />
                  </div>
                  <Select
                    value={columnMapping[colIndex] || "ignore"}
                    onValueChange={(value) => setColumnMapping((prev) => ({ ...prev, [colIndex]: value }))}
                  >
                    <SelectTrigger className="h-9">
                      <SelectValue placeholder="选择目标字段" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="ignore">忽略此列</SelectItem>
                      {schema.fields.map((field) => (
                        <SelectItem key={field.id} value={field.name}>
                          {field.displayName} ({field.name})
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              ))}
            </div>
          )}
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                取消
              </Button>
            </DialogClose>
            <Button type="button" onClick={applyColumnMapping}>
              应用映射并导入
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </Card>
  )
}
