"use client"

import type React from "react"

import { useState, useRef } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Card, CardContent } from "@/components/ui/card"
import { Textarea } from "@/components/ui/textarea"
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group"
import { FileUp, Type, Trash2, FileText, LinkIcon } from "lucide-react"
import type { DataSource } from "./ai-data-entry-wizard"
import { v4 as uuidv4 } from "uuid"

interface SourceSelectionStepProps {
  onComplete: (sources: DataSource[], mode: "one-to-one" | "batch") => void
}

export default function SourceSelectionStep({ onComplete }: SourceSelectionStepProps) {
  const [activeTab, setActiveTab] = useState("files")
  const [dataSources, setDataSources] = useState<DataSource[]>([])
  const [url, setUrl] = useState("")
  const [text, setText] = useState("")
  const [extractionMode, setExtractionMode] = useState<"one-to-one" | "batch">("one-to-one")
  const fileInputRef = useRef<HTMLInputElement>(null)

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files || e.target.files.length === 0) return

    const newSources: DataSource[] = []

    for (let i = 0; i < e.target.files.length; i++) {
      const file = e.target.files[i]

      // 读取文件内容
      const content = await readFileContent(file)

      newSources.push({
        id: uuidv4(),
        type: "file",
        name: file.name,
        content,
        size: file.size,
        mimeType: file.type,
      })
    }

    setDataSources([...dataSources, ...newSources])

    // 清空文件输入，以便可以再次选择相同的文件
    if (fileInputRef.current) {
      fileInputRef.current.value = ""
    }
  }

  const readFileContent = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
      const reader = new FileReader()

      reader.onload = (event) => {
        if (event.target?.result) {
          resolve(event.target.result as string)
        } else {
          reject(new Error("Failed to read file"))
        }
      }

      reader.onerror = () => {
        reject(new Error("Failed to read file"))
      }

      if (file.type.startsWith("text/") || file.type === "application/json") {
        reader.readAsText(file)
      } else {
        reader.readAsDataURL(file)
      }
    })
  }

  const handleAddUrl = () => {
    if (!url.trim()) return

    setDataSources([
      ...dataSources,
      {
        id: uuidv4(),
        type: "url",
        name: url,
        content: "",
        url,
      },
    ])

    setUrl("")
  }

  const handleAddText = () => {
    if (!text.trim()) return

    setDataSources([
      ...dataSources,
      {
        id: uuidv4(),
        type: "text",
        name: `文本 ${dataSources.filter((s) => s.type === "text").length + 1}`,
        content: text,
      },
    ])

    setText("")
  }

  const handleRemoveSource = (id: string) => {
    setDataSources(dataSources.filter((source) => source.id !== id))
  }

  const handleContinue = () => {
    if (dataSources.length > 0) {
      onComplete(dataSources, extractionMode)
    }
  }

  return (
    <div className="space-y-6">
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-3">
          <TabsTrigger value="files">文件</TabsTrigger>
          <TabsTrigger value="urls">网页 URL</TabsTrigger>
          <TabsTrigger value="text">文本内容</TabsTrigger>
        </TabsList>

        <TabsContent value="files" className="space-y-4 pt-4">
          <div className="flex flex-col items-center justify-center border-2 border-dashed rounded-lg p-12 text-center">
            <FileUp className="h-8 w-8 mb-4 text-muted-foreground" />
            <div className="space-y-2">
              <h3 className="font-medium">上传文件</h3>
              <p className="text-sm text-muted-foreground">拖放文件到此处，或点击下方按钮选择文件</p>
              <Input
                ref={fileInputRef}
                type="file"
                multiple
                className="hidden"
                onChange={handleFileChange}
                id="file-upload"
              />
              <Button asChild>
                <label htmlFor="file-upload">选择文件</label>
              </Button>
            </div>
          </div>
        </TabsContent>

        <TabsContent value="urls" className="space-y-4 pt-4">
          <div className="space-y-2">
            <Label htmlFor="url-input">输入网页 URL</Label>
            <div className="flex gap-2">
              <Input
                id="url-input"
                placeholder="https://example.com/page"
                value={url}
                onChange={(e) => setUrl(e.target.value)}
              />
              <Button onClick={handleAddUrl} disabled={!url.trim()}>
                添加
              </Button>
            </div>
            <p className="text-sm text-muted-foreground">添加网页 URL，系统将自动抓取内容并提取数据</p>
          </div>
        </TabsContent>

        <TabsContent value="text" className="space-y-4 pt-4">
          <div className="space-y-2">
            <Label htmlFor="text-input">输入文本内容</Label>
            <Textarea
              id="text-input"
              placeholder="粘贴包含目标数据的文本内容..."
              rows={8}
              value={text}
              onChange={(e) => setText(e.target.value)}
            />
            <Button onClick={handleAddText} disabled={!text.trim()} className="w-full">
              添加文本
            </Button>
          </div>
        </TabsContent>
      </Tabs>

      {dataSources.length > 0 && (
        <>
          <div className="space-y-2">
            <h3 className="text-lg font-medium">已添加的数据源</h3>
            <div className="space-y-2">
              {dataSources.map((source) => (
                <Card key={source.id}>
                  <CardContent className="p-4 flex items-center justify-between">
                    <div className="flex items-center gap-3">
                      {source.type === "file" && <FileText className="h-5 w-5 text-blue-500" />}
                      {source.type === "url" && <LinkIcon className="h-5 w-5 text-green-500" />}
                      {source.type === "text" && <Type className="h-5 w-5 text-purple-500" />}
                      <div>
                        <div className="font-medium">{source.name}</div>
                        <div className="text-sm text-muted-foreground">
                          {source.type === "file" && `${(source.size! / 1024).toFixed(2)} KB`}
                          {source.type === "url" && source.url}
                          {source.type === "text" && `${source.content.length} 字符`}
                        </div>
                      </div>
                    </div>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleRemoveSource(source.id)}
                      className="h-8 w-8 p-0"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  </CardContent>
                </Card>
              ))}
            </div>
          </div>

          <div className="space-y-2">
            <h3 className="text-lg font-medium">提取模式</h3>
            <RadioGroup
              value={extractionMode}
              onValueChange={(value) => setExtractionMode(value as "one-to-one" | "batch")}
            >
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="one-to-one" id="one-to-one" />
                <Label htmlFor="one-to-one">一对一提取 (每个数据源生成一条记录)</Label>
              </div>
              <div className="flex items-center space-x-2">
                <RadioGroupItem value="batch" id="batch" />
                <Label htmlFor="batch">批量提取 (从每个数据源提取多条记录)</Label>
              </div>
            </RadioGroup>
          </div>
          <Button onClick={handleContinue} className="w-full">
            继续下一步
          </Button>
        </>
      )}
    </div>
  )
}
