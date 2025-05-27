"use client"

import { useState } from "react"
import { Steps, Step } from "@/components/ui/steps"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Button } from "@/components/ui/button"
import { ArrowRight, ArrowLeft, Save } from "lucide-react"
import BasicInfoStep from "@/components/ai-inference/steps/basic-info-step"
import QueryDefinitionStep from "@/components/ai-inference/steps/query-definition-step"
import DataCombinationStep from "@/components/ai-inference/steps/data-combination-step"
import PromptTemplateStep from "@/components/ai-inference/steps/prompt-template-step"
import ExecutionConstraintsStep from "@/components/ai-inference/steps/execution-constraints-step"
import ReviewStep from "@/components/ai-inference/steps/review-step"
import React from "react"

// 定义推理事务定义的类型
export type ReasoningTransactionDefinition = {
  id?: string
  name: string
  description: string
  subjectSchemaName?: string
  queryDefinitions: QueryDefinition[]
  promptTemplate: PromptTemplateDefinition
  dataCombinationRules: DataCombinationRule[]
  executionConstraints: ExecutionConstraints
}

// 查询定义类型
export type QueryDefinition = {
  queryId: string
  outputViewName: string
  sourceSchemaName: string
  filterExpression: string
  selectFields: string[]
}

// Prompt模板定义类型
export type PromptTemplateDefinition = {
  templateContent: string
  expectedInputViewNames: string[]
}

// 数据组合规则类型
export type DataCombinationRule = {
  viewNamesToCrossProduct: string[]
  singletonViewNamesForContext: string[]
  maxCombinations: number
  strategy: "CrossProduct" | "RandomSampling" | "PrioritySampling"
  samplingRule?: {
    priorityField: string
    preferHigherValues: boolean
    randomSeed: number
  }
}

// 执行约束类型
export type ExecutionConstraints = {
  maxEstimatedCostUSD: number
  maxExecutionTimeMinutes: number
  maxConcurrentAICalls: number
  enableBatching: boolean
  batchSize: number
}

// 可用的Schema列表（模拟数据）
export const availableSchemas = [
  { name: "game_character", displayName: "游戏角色", fieldCount: 12 },
  { name: "game_item", displayName: "游戏道具", fieldCount: 8 },
  { name: "game_quest", displayName: "游戏任务", fieldCount: 10 },
  { name: "game_scene", displayName: "游戏场景", fieldCount: 15 },
  { name: "game_dialogue", displayName: "游戏对话", fieldCount: 7 },
]

// 模拟获取Schema字段
export const getSchemaFields = (schemaName: string) => {
  const fieldMaps: Record<string, { name: string; displayName: string; type: string }[]> = {
    game_character: [
      { name: "id", displayName: "ID", type: "string" },
      { name: "name", displayName: "角色名称", type: "string" },
      { name: "race", displayName: "种族", type: "string" },
      { name: "class", displayName: "职业", type: "string" },
      { name: "level", displayName: "等级", type: "number" },
      { name: "health", displayName: "生命值", type: "number" },
      { name: "mana", displayName: "魔法值", type: "number" },
      { name: "strength", displayName: "力量", type: "number" },
      { name: "dexterity", displayName: "敏捷", type: "number" },
      { name: "intelligence", displayName: "智力", type: "number" },
      { name: "background", displayName: "背景故事", type: "text" },
      { name: "status", displayName: "状态", type: "string" },
    ],
    game_scene: [
      { name: "id", displayName: "ID", type: "string" },
      { name: "name", displayName: "场景名称", type: "string" },
      { name: "description", displayName: "描述", type: "text" },
      { name: "environment", displayName: "环境类型", type: "string" },
      { name: "mood", displayName: "氛围", type: "string" },
      { name: "time_of_day", displayName: "时间", type: "string" },
      { name: "weather", displayName: "天气", type: "string" },
      { name: "danger_level", displayName: "危险等级", type: "number" },
      { name: "background_music", displayName: "背景音乐", type: "string" },
      { name: "is_indoor", displayName: "是否室内", type: "boolean" },
      { name: "region", displayName: "所属区域", type: "string" },
      { name: "points_of_interest", displayName: "兴趣点", type: "array" },
      { name: "available_exits", displayName: "可用出口", type: "array" },
      { name: "special_effects", displayName: "特殊效果", type: "array" },
      { name: "status", displayName: "状态", type: "string" },
    ],
    game_dialogue: [
      { name: "id", displayName: "ID", type: "string" },
      { name: "speaker", displayName: "说话者", type: "string" },
      { name: "context", displayName: "上下文", type: "string" },
      { name: "emotion", displayName: "情绪", type: "string" },
      { name: "topic", displayName: "主题", type: "string" },
      { name: "importance", displayName: "重要性", type: "number" },
      { name: "is_quest_related", displayName: "是否任务相关", type: "boolean" },
    ],
    game_quest: [
      { name: "id", displayName: "ID", type: "string" },
      { name: "title", displayName: "任务标题", type: "string" },
      { name: "description", displayName: "描述", type: "text" },
      { name: "difficulty", displayName: "难度", type: "number" },
      { name: "reward_type", displayName: "奖励类型", type: "string" },
      { name: "reward_amount", displayName: "奖励数量", type: "number" },
      { name: "giver_id", displayName: "任务给予者ID", type: "string" },
      { name: "location_id", displayName: "任务地点ID", type: "string" },
      { name: "prerequisites", displayName: "前置条件", type: "array" },
      { name: "status", displayName: "状态", type: "string" },
    ],
    game_item: [
      { name: "id", displayName: "ID", type: "string" },
      { name: "name", displayName: "物品名称", type: "string" },
      { name: "description", displayName: "描述", type: "text" },
      { name: "type", displayName: "类型", type: "string" },
      { name: "rarity", displayName: "稀有度", type: "string" },
      { name: "value", displayName: "价值", type: "number" },
      { name: "weight", displayName: "重量", type: "number" },
      { name: "effects", displayName: "效果", type: "array" },
    ],
  }

  return fieldMaps[schemaName] || []
}

export default function ReasoningTransactionCreationWizard() {
  const [currentStep, setCurrentStep] = useState(0)
  const [transactionDefinition, setTransactionDefinition] = useState<ReasoningTransactionDefinition>({
    name: "",
    description: "",
    queryDefinitions: [],
    promptTemplate: {
      templateContent: "",
      expectedInputViewNames: [],
    },
    dataCombinationRules: [
      {
        viewNamesToCrossProduct: [],
        singletonViewNamesForContext: [],
        maxCombinations: 1000,
        strategy: "CrossProduct",
      },
    ],
    executionConstraints: {
      maxEstimatedCostUSD: 10,
      maxExecutionTimeMinutes: 30,
      maxConcurrentAICalls: 5,
      enableBatching: true,
      batchSize: 10,
    },
  })

  // Add a memoization for the updateTransactionDefinition function to prevent unnecessary re-renders
  const updateTransactionDefinition = React.useCallback((updates: Partial<ReasoningTransactionDefinition>) => {
    setTransactionDefinition((prev) => ({
      ...prev,
      ...updates,
    }))
  }, [])

  const goToNextStep = () => {
    setCurrentStep(currentStep + 1)
  }

  const goToPreviousStep = () => {
    setCurrentStep(currentStep - 1)
  }

  const handleSaveTransaction = () => {
    // 在实际应用中，这里会将事务定义保存到数据库
    console.log("保存推理事务定义:", transactionDefinition)
    // 可以添加保存成功后的导航逻辑
  }

  // 检查当前步骤是否可以进入下一步
  const canProceedToNextStep = () => {
    switch (currentStep) {
      case 0: // 基本信息
        return transactionDefinition.name.trim() !== "" && transactionDefinition.description.trim() !== ""
      case 1: // 查询定义
        return transactionDefinition.queryDefinitions.length > 0
      case 2: // 数据组合
        const rule = transactionDefinition.dataCombinationRules[0]
        return rule.viewNamesToCrossProduct.length > 0 || rule.singletonViewNamesForContext.length > 0
      case 3: // Prompt模板
        return (
          transactionDefinition.promptTemplate.templateContent.trim() !== "" &&
          transactionDefinition.promptTemplate.expectedInputViewNames.length > 0
        )
      case 4: // 执行约束
        return true // 执行约束有默认值，总是可以进入下一步
      default:
        return true
    }
  }

  return (
    <div className="space-y-6">
      <Steps currentStep={currentStep}>
        <Step title="基本信息" description="定义推理事务的基本信息" />
        <Step title="数据查询" description="定义数据视图查询" />
        <Step title="数据组合" description="设置数据组合规则" />
        <Step title="Prompt模板" description="创建Prompt模板" />
        <Step title="执行约束" description="设置执行限制" />
        <Step title="确认" description="确认并保存" />
      </Steps>

      <Card>
        <CardHeader>
          <CardTitle>
            {currentStep === 0 && "基本信息"}
            {currentStep === 1 && "数据查询定义"}
            {currentStep === 2 && "数据组合规则"}
            {currentStep === 3 && "Prompt模板设计"}
            {currentStep === 4 && "执行约束设置"}
            {currentStep === 5 && "确认推理事务定义"}
          </CardTitle>
          <CardDescription>
            {currentStep === 0 && "设置推理事务的名称、描述和主体Schema"}
            {currentStep === 1 && "定义从哪些数据源获取数据，以及如何筛选和选择字段"}
            {currentStep === 2 && "设置如何组合多个数据视图，包括叉积规则和单例上下文"}
            {currentStep === 3 && "创建用于AI生成的Prompt模板，使用占位符引用数据视图中的字段"}
            {currentStep === 4 && "设置执行限制，如最大成本、执行时间和并发调用数"}
            {currentStep === 5 && "确认所有设置并保存推理事务定义"}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {currentStep === 0 && (
            <BasicInfoStep
              transactionDefinition={transactionDefinition}
              updateTransactionDefinition={updateTransactionDefinition}
              availableSchemas={availableSchemas}
            />
          )}
          {currentStep === 1 && (
            <QueryDefinitionStep
              transactionDefinition={transactionDefinition}
              updateTransactionDefinition={updateTransactionDefinition}
              availableSchemas={availableSchemas}
              getSchemaFields={getSchemaFields}
            />
          )}
          {currentStep === 2 && (
            <DataCombinationStep
              transactionDefinition={transactionDefinition}
              updateTransactionDefinition={updateTransactionDefinition}
            />
          )}
          {currentStep === 3 && (
            <PromptTemplateStep
              transactionDefinition={transactionDefinition}
              updateTransactionDefinition={updateTransactionDefinition}
            />
          )}
          {currentStep === 4 && (
            <ExecutionConstraintsStep
              transactionDefinition={transactionDefinition}
              updateTransactionDefinition={updateTransactionDefinition}
            />
          )}
          {currentStep === 5 && <ReviewStep transactionDefinition={transactionDefinition} />}
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

          {currentStep < 5 ? (
            <Button onClick={goToNextStep} disabled={!canProceedToNextStep()}>
              下一步
              <ArrowRight className="ml-2 h-4 w-4" />
            </Button>
          ) : (
            <Button onClick={handleSaveTransaction}>
              <Save className="mr-2 h-4 w-4" />
              保存推理事务
            </Button>
          )}
        </CardFooter>
      </Card>
    </div>
  )
}
