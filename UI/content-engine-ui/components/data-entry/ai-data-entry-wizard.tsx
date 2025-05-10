"use client"

import { useState, useEffect } from "react"
import { Steps, Step } from "@/components/ui/steps"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { ArrowRight, ArrowLeft, Save } from "lucide-react"
import SourceSelectionStep from "@/components/data-entry/source-selection-step"
import MappingConfigStep from "@/components/data-entry/mapping-config-step"
import ExtractionPreviewStep from "@/components/data-entry/extraction-preview-step"
import ResultsReviewStep from "@/components/data-entry/results-review-step"

// 定义 Schema 类型
type Field = {
  id: number
  name: string
  displayName: string
  type: string
  required: boolean
}

type Schema = {
  id: string
  name: string
  description: string
  fields: Field[]
}

// 定义数据源类型
export type DataSource = {
  id: string
  type: "file" | "url" | "text"
  name: string
  content: string
  size?: number
  mimeType?: string
  url?: string
}

// 定义提取结果类型
export type ExtractionResult = {
  sourceId: string
  records: Record<string, any>[]
  status: "pending" | "processing" | "success" | "error"
  error?: string
}

export default function AIDataEntryWizard({ schema }: { schema: Schema }) {
  const [currentStep, setCurrentStep] = useState(0)
  const [dataSources, setDataSources] = useState<DataSource[]>([])
  const [extractionMode, setExtractionMode] = useState<"one-to-one" | "batch">("one-to-one")
  const [fieldMappings, setFieldMappings] = useState<Record<string, string>>({})
  const [extractionResults, setExtractionResults] = useState<ExtractionResult[]>([])
  const [finalRecords, setFinalRecords] = useState<Record<string, any>[]>([])
  const [isExtracting, setIsExtracting] = useState(false)
  const [autoExtractionInitiated, setAutoExtractionInitiated] = useState(false)

  // 自动开始提取过程
  useEffect(() => {
    // 只在首次进入提取步骤时自动开始提取，并且只有当有待处理的结果时
    if (
      currentStep === 2 &&
      !autoExtractionInitiated &&
      extractionResults.some((r) => r.status === "pending") &&
      !isExtracting
    ) {
      setAutoExtractionInitiated(true)
      // 模拟自动开始提取
      const timer = setTimeout(() => {
        startExtraction()
      }, 500)

      return () => clearTimeout(timer)
    }
  }, [currentStep, autoExtractionInitiated, isExtracting])

  // 模拟提取过程
  const startExtraction = async () => {
    setIsExtracting(true)

    // 更新所有待处理的结果为处理中
    const processingResults = extractionResults.map((result) =>
      result.status === "pending"
        ? {
            ...result,
            status: "processing",
          }
        : result,
    )

    setExtractionResults(processingResults)

    // 模拟 AI 处理延迟
    await new Promise((resolve) => setTimeout(resolve, 2000))

    // 生成一些示例数据，确保有内容可以显示
    const updatedResults = processingResults.map((result) => {
      if (result.status !== "processing") return result

      const source = dataSources.find((s) => s.id === result.sourceId)
      if (!source) return { ...result, status: "error", error: "数据源不存在" }

      try {
        // 确保每个数据源至少生成一条记录
        const recordCount = extractionMode === "one-to-one" ? 1 : Math.floor(Math.random() * 3) + 2
        const extractedRecords = Array.from({ length: recordCount }, (_, i) => generateMockRecord(schema, source, i))

        return {
          ...result,
          records: extractedRecords,
          status: "success",
        }
      } catch (error) {
        return {
          ...result,
          status: "error",
          error: "提取失败：" + (error instanceof Error ? error.message : String(error)),
        }
      }
    })

    // 确保状态更新
    setExtractionResults(updatedResults)
    setIsExtracting(false)

    // 如果有成功提取的结果，合并所有记录
    const allRecords = updatedResults.flatMap((result) => (result.status === "success" ? result.records : []))
    setFinalRecords(allRecords)
  }

  // 生成模拟记录
  const generateMockRecord = (schema: Schema, source: DataSource, index: number): Record<string, any> => {
    // 这里根据 schema 和 source 生成模拟数据
    const record: Record<string, any> = {}

    schema.fields.forEach((field) => {
      switch (field.type) {
        case "string":
          if (field.name === "name") {
            record[field.name] = source.type === "file" ? `角色${index + 1}` : `艾莉娅${index + 1}`
          } else if (field.name === "race") {
            record[field.name] = ["人类", "精灵", "矮人", "兽人"][Math.floor(Math.random() * 4)]
          } else if (field.name === "class") {
            record[field.name] = ["战士", "法师", "盗贼", "牧师"][Math.floor(Math.random() * 4)]
          } else {
            record[field.name] = `${field.displayName}示例值${index + 1}`
          }
          break
        case "number":
          if (field.name === "level") {
            record[field.name] = Math.floor(Math.random() * 30) + 1
          } else if (field.name === "health") {
            record[field.name] = (Math.floor(Math.random() * 10) + 1) * 100
          } else {
            record[field.name] = Math.floor(Math.random() * 100)
          }
          break
        case "array":
          if (field.name === "skills") {
            record[field.name] = [
              { name: "火球术", level: 3 },
              { name: "治疗术", level: 2 },
              { name: "隐身术", level: 1 },
            ].slice(0, Math.floor(Math.random() * 3) + 1)
          } else {
            record[field.name] = [`项目1`, `项目2`, `项目3`].slice(0, Math.floor(Math.random() * 3) + 1)
          }
          break
        case "text":
          if (field.name === "background") {
            record[field.name] = `这是一个来自${
              record["race"] || "未知种族"
            }的${record["class"] || "冒险者"}，拥有丰富的冒险经历...`
          } else {
            record[field.name] = `这是${field.displayName}的详细描述内容...`
          }
          break
        default:
          record[field.name] = `${field.displayName}默认值`
      }
    })

    return record
  }

  function goToNextStep() {
    // 检查当前步骤是否可以进入下一步
    if (
      (currentStep === 0 && dataSources.length === 0) ||
      (currentStep === 1 && Object.keys(fieldMappings).length === 0) ||
      (currentStep === 2 && finalRecords.length === 0 && extractionResults.every((r) => r.status !== "success"))
    ) {
      return // 如果条件不满足，不允许进入下一步
    }
    setCurrentStep(currentStep + 1)
  }

  const goToPreviousStep = () => {
    setCurrentStep(currentStep - 1)
  }

  const handleSourcesSelected = (sources: DataSource[], mode: "one-to-one" | "batch") => {
    setDataSources(sources)
    setExtractionMode(mode)

    // 初始化提取结果
    const initialResults = sources.map((source) => ({
      sourceId: source.id,
      records: [],
      status: "pending" as const,
    }))
    setExtractionResults(initialResults)

    goToNextStep()
  }

  const handleMappingConfigured = (mappings: Record<string, string>) => {
    setFieldMappings(mappings)
    setAutoExtractionInitiated(false) // 重置标志，以便下次进入提取步骤时可以再次自动开始
    goToNextStep()
  }

  const handleExtractionComplete = (results: ExtractionResult[]) => {
    setExtractionResults(results)

    // 合并所有成功提取的记录
    const allRecords = results.flatMap((result) => (result.status === "success" ? result.records : []))
    setFinalRecords(allRecords)

    goToNextStep()
  }

  const handleSaveRecords = () => {
    // 在实际应用中，这里会将记录保存到数据库
    console.log("保存记录:", finalRecords)
    // 可以添加保存成功后的导航逻辑
  }

  return (
    <div className="space-y-6">
      <Steps currentStep={currentStep}>
        <Step title="选择数据源" description="选择要处理的文件或网页" />
        <Step title="配置映射" description="设置字段映射规则" />
        <Step title="AI 提取预览" description="预览和调整 AI 提取结果" />
        <Step title="确认保存" description="确认并保存数据" />
      </Steps>

      <Card>
        <CardHeader>
          <CardTitle>
            {currentStep === 0 && "选择数据源"}
            {currentStep === 1 && "配置字段映射"}
            {currentStep === 2 && "AI 提取预览"}
            {currentStep === 3 && "确认保存数据"}
          </CardTitle>
          <CardDescription>
            {currentStep === 0 && "选择要处理的文件、网页或文本内容"}
            {currentStep === 1 && "配置源数据与目标字段的映射规则"}
            {currentStep === 2 && "预览 AI 提取的数据并进行必要的调整"}
            {currentStep === 3 && "确认提取的数据并保存到系统"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {currentStep === 0 && <SourceSelectionStep onComplete={handleSourcesSelected} />}
          {currentStep === 1 && (
            <MappingConfigStep
              schema={schema}
              dataSources={dataSources}
              extractionMode={extractionMode}
              onComplete={handleMappingConfigured}
            />
          )}
          {currentStep === 2 && (
            <ExtractionPreviewStep
              schema={schema}
              dataSources={dataSources}
              fieldMappings={fieldMappings}
              extractionMode={extractionMode}
              extractionResults={extractionResults}
              onComplete={handleExtractionComplete}
              isExtracting={isExtracting}
              startExtraction={startExtraction}
            />
          )}
          {currentStep === 3 && <ResultsReviewStep schema={schema} records={finalRecords} />}
        </CardContent>
        <CardFooter className="flex justify-between">
          {currentStep > 0 ? (
            <Button variant="outline" onClick={goToPreviousStep}>
              <ArrowLeft className="mr-2 h-4 w-4" />
              上一步
            </Button>
          ) : (
            <div></div>
          )}

          {currentStep < 3 ? (
            currentStep === 0 ? (
              <Button
                onClick={() => {
                  if (dataSources.length > 0) {
                    goToNextStep()
                  }
                }}
                disabled={dataSources.length === 0}
              >
                下一步
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            ) : currentStep === 2 ? (
              <Button
                onClick={goToNextStep}
                disabled={finalRecords.length === 0 && extractionResults.every((r) => r.status !== "success")}
              >
                下一步
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            ) : (
              <Button onClick={goToNextStep} disabled={Object.keys(fieldMappings).length === 0}>
                下一步
                <ArrowRight className="ml-2 h-4 w-4" />
              </Button>
            )
          ) : (
            <Button onClick={handleSaveRecords}>
              <Save className="mr-2 h-4 w-4" />
              保存数据
            </Button>
          )}
        </CardFooter>
      </Card>
    </div>
  )
}
