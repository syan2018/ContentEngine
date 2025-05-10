"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { CheckCircle, AlertCircle, Eye, Trash2 } from "lucide-react"
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle } from "@/components/ui/dialog"

interface ResultsReviewStepProps {
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
  records: Record<string, any>[]
}

export default function ResultsReviewStep({ schema, records }: ResultsReviewStepProps) {
  const [selectedRecords, setSelectedRecords] = useState<Record<string, any>[]>(records)
  const [viewRecord, setViewRecord] = useState<Record<string, any> | null>(null)

  const handleRemoveRecord = (index: number) => {
    setSelectedRecords(selectedRecords.filter((_, i) => i !== index))
  }

  const getFieldPreview = (record: Record<string, any>, field: ResultsReviewStepProps["schema"]["fields"][0]) => {
    const value = record[field.name]

    if (value === undefined || value === null) {
      return <span className="text-muted-foreground">空</span>
    }

    switch (field.type) {
      case "string":
      case "number":
        return String(value)
      case "text":
        return value.length > 50 ? value.substring(0, 50) + "..." : value
      case "array":
        return Array.isArray(value) ? `[${value.length} 项]` : String(value)
      default:
        return String(value)
    }
  }

  const getValidationStatus = (record: Record<string, any>) => {
    const missingRequiredFields = schema.fields
      .filter((field) => field.required)
      .filter((field) => record[field.name] === undefined || record[field.name] === null || record[field.name] === "")

    if (missingRequiredFields.length > 0) {
      return {
        valid: false,
        message: `缺少必填字段: ${missingRequiredFields.map((f) => f.displayName).join(", ")}`,
      }
    }

    return { valid: true, message: "数据有效" }
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-medium">提取结果确认</h3>
        <Badge>{selectedRecords.length} 条记录</Badge>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>数据验证摘要</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="flex flex-col items-center p-4 bg-muted rounded-lg">
              <div className="text-2xl font-bold">{selectedRecords.length}</div>
              <div className="text-sm text-muted-foreground">总记录数</div>
            </div>
            <div className="flex flex-col items-center p-4 bg-green-50 text-green-700 rounded-lg">
              <div className="text-2xl font-bold">
                {selectedRecords.filter((record) => getValidationStatus(record).valid).length}
              </div>
              <div className="text-sm">有效记录</div>
            </div>
            <div className="flex flex-col items-center p-4 bg-red-50 text-red-700 rounded-lg">
              <div className="text-2xl font-bold">
                {selectedRecords.filter((record) => !getValidationStatus(record).valid).length}
              </div>
              <div className="text-sm">无效记录</div>
            </div>
          </div>
        </CardContent>
      </Card>

      <Tabs defaultValue="table" className="w-full">
        <TabsList className="grid w-full grid-cols-2">
          <TabsTrigger value="table">表格视图</TabsTrigger>
          <TabsTrigger value="json">JSON 视图</TabsTrigger>
        </TabsList>
        <TabsContent value="table" className="pt-4">
          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[50px]">#</TableHead>
                  {schema.fields.slice(0, 4).map((field) => (
                    <TableHead key={field.id}>{field.displayName}</TableHead>
                  ))}
                  <TableHead>状态</TableHead>
                  <TableHead className="w-[100px]">操作</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {selectedRecords.map((record, index) => {
                  const validationStatus = getValidationStatus(record)

                  return (
                    <TableRow key={index}>
                      <TableCell className="font-medium">{index + 1}</TableCell>
                      {schema.fields.slice(0, 4).map((field) => (
                        <TableCell key={field.id}>{getFieldPreview(record, field)}</TableCell>
                      ))}
                      <TableCell>
                        {validationStatus.valid ? (
                          <Badge variant="outline" className="bg-green-50 text-green-700 border-green-200">
                            <CheckCircle className="h-3 w-3 mr-1" />
                            有效
                          </Badge>
                        ) : (
                          <Badge variant="outline" className="bg-red-50 text-red-700 border-red-200">
                            <AlertCircle className="h-3 w-3 mr-1" />
                            无效
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        <div className="flex items-center gap-2">
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 w-8 p-0"
                            onClick={() => setViewRecord(record)}
                          >
                            <Eye className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 w-8 p-0"
                            onClick={() => handleRemoveRecord(index)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          </div>
        </TabsContent>
        <TabsContent value="json" className="pt-4">
          <Card>
            <CardContent className="p-4">
              <pre className="text-xs bg-muted p-4 rounded-md overflow-auto max-h-[400px]">
                {JSON.stringify(selectedRecords, null, 2)}
              </pre>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {viewRecord && (
        <Dialog open={!!viewRecord} onOpenChange={() => setViewRecord(null)}>
          <DialogContent className="max-w-3xl max-h-[80vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>记录详情</DialogTitle>
              <DialogDescription>
                {getValidationStatus(viewRecord).valid ? "数据有效" : getValidationStatus(viewRecord).message}
              </DialogDescription>
            </DialogHeader>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4 py-4">
              {schema.fields.map((field) => (
                <div key={field.id} className="space-y-1">
                  <div className="text-sm font-medium">{field.displayName}</div>
                  <div className="bg-muted p-2 rounded-md">
                    {field.type === "array" ? (
                      <pre className="text-xs whitespace-pre-wrap overflow-auto max-h-[200px]">
                        {JSON.stringify(viewRecord[field.name] || [], null, 2)}
                      </pre>
                    ) : field.type === "text" ? (
                      <div className="text-sm whitespace-pre-wrap">{viewRecord[field.name] || ""}</div>
                    ) : (
                      <div className="text-sm">{viewRecord[field.name] || ""}</div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </DialogContent>
        </Dialog>
      )}
    </div>
  )
}
