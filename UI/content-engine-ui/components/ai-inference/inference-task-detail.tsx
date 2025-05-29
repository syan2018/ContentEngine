"use client"

import { useState } from "react"
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Button } from "@/components/ui/button"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import {
  Database,
  ArrowRightLeft,
  FileText,
  Zap,
  Play,
  Pause,
  RotateCcw,
  Eye,
  Download,
  Clock,
  DollarSign,
} from "lucide-react"
import type { ReasoningTransactionDefinition } from "./reasoning-transaction-creation-wizard"

// 模拟推理任务数据
const getMockTaskData = (
  taskId: string,
): ReasoningTransactionDefinition & {
  id: string
  status: "running" | "completed" | "paused" | "failed"
  createdAt: string
  lastExecutedAt?: string
  totalRequests: number
  completedRequests: number
  estimatedCost: number
  actualCost: number
} => {
  const baseTask = {
    id: taskId,
    name: "NPC对话生成",
    description: "基于角色特性和场景环境生成符合角色性格的对话内容，用于丰富游戏世界的交互体验。",
    subjectSchemaName: "game_character",
    queryDefinitions: [
      {
        queryId: "char-query-1",
        outputViewName: "CharacterView",
        sourceSchemaName: "game_character",
        filterExpression: "$.status == 'Active' && $.level > 10",
        selectFields: ["id", "name", "race", "class", "level", "background", "personality"],
      },
      {
        queryId: "scene-query-1",
        outputViewName: "SceneView",
        sourceSchemaName: "game_scene",
        filterExpression: "$.status == 'Active'",
        selectFields: ["id", "name", "description", "environment", "mood", "time_of_day"],
      },
    ],
    promptTemplate: {
      templateContent: `你是一个专业的游戏对话编剧。请为以下角色在指定场景中生成一段符合其性格特点的对话。

角色信息：
- 姓名：{{CharacterView.name}}
- 种族：{{CharacterView.race}}
- 职业：{{CharacterView.class}}
- 等级：{{CharacterView.level}}
- 背景故事：{{CharacterView.background}}

场景信息：
- 场景名称：{{SceneView.name}}
- 场景描述：{{SceneView.description}}
- 环境类型：{{SceneView.environment}}
- 氛围：{{SceneView.mood}}
- 时间：{{SceneView.time_of_day}}

请生成一段3-5句的对话，要求：
1. 符合角色的种族和职业特点
2. 体现角色的等级和经验
3. 适应当前场景的氛围
4. 语言风格要有个性化特色

对话内容：`,
      expectedInputViewNames: ["CharacterView", "SceneView"],
    },
    dataCombinationRules: [
      {
        viewNamesToCrossProduct: ["CharacterView", "SceneView"],
        singletonViewNamesForContext: [],
        maxCombinations: 100,
        strategy: "CrossProduct" as const,
      },
    ],
    executionConstraints: {
      maxEstimatedCostUSD: 25,
      maxExecutionTimeMinutes: 60,
      maxConcurrentAICalls: 3,
      enableBatching: true,
      batchSize: 5,
    },
    status: taskId === "1" ? "completed" : taskId === "2" ? "running" : "paused",
    createdAt: "2023-05-01 14:30:22",
    lastExecutedAt: taskId === "1" ? "2023-05-01 15:45:33" : taskId === "2" ? "2023-05-02 10:15:00" : undefined,
    totalRequests: 48,
    completedRequests: taskId === "1" ? 48 : taskId === "2" ? 32 : 15,
    estimatedCost: 2.4,
    actualCost: taskId === "1" ? 2.35 : taskId === "2" ? 1.6 : 0.75,
  }

  return baseTask
}

// 模拟叉积请求数据
const getMockCrossProductRequests = (taskId: string) => {
  const characters = [
    { id: "char1", name: "艾莉娅", race: "精灵", class: "法师", level: 25, background: "来自古老精灵家族的法师" },
    { id: "char2", name: "托尔", race: "矮人", class: "战士", level: 30, background: "经验丰富的矮人战士" },
    { id: "char3", name: "莱克斯", race: "人类", class: "盗贼", level: 22, background: "机敏的人类盗贼" },
  ]

  const scenes = [
    { id: "scene1", name: "魔法森林", description: "充满神秘能量的古老森林", environment: "森林", mood: "神秘" },
    { id: "scene2", name: "酒馆大厅", description: "热闹的冒险者聚集地", environment: "室内", mood: "热闹" },
  ]

  const requests = []
  let requestId = 1

  for (const char of characters) {
    for (const scene of scenes) {
      const status =
        taskId === "1"
          ? "completed"
          : taskId === "2" && requestId <= 4
            ? "completed"
            : taskId === "2" && requestId <= 6
              ? "running"
              : "pending"

      requests.push({
        id: `req-${requestId}`,
        characterData: char,
        sceneData: scene,
        status,
        generatedContent: status === "completed" ? generateMockDialogue(char, scene) : null,
        executedAt:
          status === "completed"
            ? `2023-05-0${Math.ceil(requestId / 10)} ${10 + requestId}:${15 + ((requestId * 3) % 45)}:${20 + ((requestId * 7) % 40)}`
            : null,
        cost: status === "completed" ? 0.04 + Math.random() * 0.02 : 0,
      })
      requestId++
    }
  }

  return requests.slice(0, taskId === "1" ? 6 : taskId === "2" ? 6 : 4)
}

// 生成模拟对话内容
const generateMockDialogue = (character: any, scene: any) => {
  const dialogues = {
    "艾莉娅-魔法森林":
      "这片森林中的魔法能量异常浓郁，我能感受到古老的咒文在空气中回响。作为精灵族的后裔，我对这种原始的魔法力量有着天然的亲近感。",
    "艾莉娅-酒馆大厅":
      "各位冒险者，我刚从魔法学院归来，听说这里有人在寻找会施法的伙伴？我的法术或许能为大家的冒险提供帮助。",
    "托尔-魔法森林":
      "哼，这些树木长得太高了，遮挡了我的视线。不过我的战斧依然锋利，无论是魔法生物还是普通野兽，都别想轻易靠近我们。",
    "托尔-酒馆大厅":
      "来一杯最烈的矮人烈酒！今天我们成功清理了一个哥布林巢穴，是时候庆祝一下了。有谁想听听我是如何一斧头劈倒哥布林首领的？",
    "莱克斯-魔法森林":
      "保持安静，我听到了不寻常的声音。这片森林里可能潜伏着危险，我们需要小心行进。我的直觉告诉我，前方可能有陷阱。",
    "莱克斯-酒馆大厅":
      "听说最近有一批珍贵的宝石从商队中失踪了？如果价格合适的话，我或许能帮忙找回来。当然，我的服务费可不便宜。",
  }

  const key = `${character.name}-${scene.name}`
  return dialogues[key] || `${character.name}在${scene.name}中说了一些话...`
}

interface InferenceTaskDetailProps {
  taskId: string
}

export default function InferenceTaskDetail({ taskId }: InferenceTaskDetailProps) {
  const [activeTab, setActiveTab] = useState("overview")
  const taskData = getMockTaskData(taskId)
  const crossProductRequests = getMockCrossProductRequests(taskId)

  const getStatusBadge = (status: string) => {
    switch (status) {
      case "completed":
        return <Badge className="bg-green-100 text-green-800 border-green-200">已完成</Badge>
      case "running":
        return <Badge className="bg-blue-100 text-blue-800 border-blue-200">运行中</Badge>
      case "paused":
        return <Badge className="bg-yellow-100 text-yellow-800 border-yellow-200">已暂停</Badge>
      case "failed":
        return <Badge className="bg-red-100 text-red-800 border-red-200">失败</Badge>
      default:
        return <Badge variant="outline">未知</Badge>
    }
  }

  const getRequestStatusBadge = (status: string) => {
    switch (status) {
      case "completed":
        return (
          <Badge variant="outline" className="bg-green-50 text-green-700 border-green-200">
            完成
          </Badge>
        )
      case "running":
        return (
          <Badge variant="outline" className="bg-blue-50 text-blue-700 border-blue-200">
            执行中
          </Badge>
        )
      case "pending":
        return (
          <Badge variant="outline" className="bg-gray-50 text-gray-700 border-gray-200">
            等待中
          </Badge>
        )
      case "failed":
        return (
          <Badge variant="outline" className="bg-red-50 text-red-700 border-red-200">
            失败
          </Badge>
        )
      default:
        return <Badge variant="outline">未知</Badge>
    }
  }

  return (
    <div className="space-y-6">
      {/* 任务基本信息 */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle className="text-2xl">{taskData.name}</CardTitle>
              <CardDescription className="mt-2">{taskData.description}</CardDescription>
            </div>
            <div className="flex items-center gap-2">
              {getStatusBadge(taskData.status)}
              <div className="flex gap-2">
                {taskData.status === "running" && (
                  <Button variant="outline" size="sm">
                    <Pause className="mr-2 h-4 w-4" />
                    暂停
                  </Button>
                )}
                {taskData.status === "paused" && (
                  <Button variant="outline" size="sm">
                    <Play className="mr-2 h-4 w-4" />
                    继续
                  </Button>
                )}
                {(taskData.status === "completed" || taskData.status === "paused") && (
                  <Button variant="outline" size="sm">
                    <RotateCcw className="mr-2 h-4 w-4" />
                    重新执行
                  </Button>
                )}
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">创建时间</p>
              <p className="font-medium">{taskData.createdAt}</p>
            </div>
            {taskData.lastExecutedAt && (
              <div className="space-y-1">
                <p className="text-sm text-muted-foreground">最后执行</p>
                <p className="font-medium">{taskData.lastExecutedAt}</p>
              </div>
            )}
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">执行进度</p>
              <p className="font-medium">
                {taskData.completedRequests} / {taskData.totalRequests}
              </p>
            </div>
            <div className="space-y-1">
              <p className="text-sm text-muted-foreground">实际成本</p>
              <p className="font-medium text-green-600">${taskData.actualCost.toFixed(2)}</p>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* 详细信息标签页 */}
      <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview">任务概览</TabsTrigger>
          <TabsTrigger value="requests">叉积请求</TabsTrigger>
          <TabsTrigger value="prompts">Prompt样例</TabsTrigger>
          <TabsTrigger value="results">执行结果</TabsTrigger>
        </TabsList>

        <TabsContent value="overview" className="space-y-6 pt-4">
          {/* 数据查询定义 */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Database className="h-5 w-5 mr-2 text-blue-500" />
                数据查询定义
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              {taskData.queryDefinitions.map((query) => (
                <Card key={query.queryId}>
                  <CardHeader className="py-3">
                    <CardTitle className="text-base">
                      视图: <span className="font-bold">{query.outputViewName}</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="py-2 space-y-2">
                    <div className="flex justify-between text-sm">
                      <span className="text-muted-foreground">数据源:</span>
                      <span>{query.sourceSchemaName}</span>
                    </div>
                    {query.filterExpression && (
                      <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">筛选条件:</span>
                        <span className="font-mono text-xs bg-muted px-2 py-1 rounded">{query.filterExpression}</span>
                      </div>
                    )}
                    <div className="text-sm">
                      <span className="text-muted-foreground">选择字段:</span>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {query.selectFields.map((field) => (
                          <Badge key={field} variant="outline">
                            {field}
                          </Badge>
                        ))}
                      </div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </CardContent>
          </Card>

          {/* 数据组合规则 */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <ArrowRightLeft className="h-5 w-5 mr-2 text-purple-500" />
                数据组合规则
              </CardTitle>
            </CardHeader>
            <CardContent>
              {taskData.dataCombinationRules.map((rule, index) => (
                <div key={index} className="space-y-4">
                  <div className="space-y-2">
                    <h4 className="text-sm font-medium">叉积视图</h4>
                    <div className="flex flex-wrap gap-1">
                      {rule.viewNamesToCrossProduct.map((viewName) => (
                        <Badge key={viewName}>{viewName}</Badge>
                      ))}
                    </div>
                  </div>
                  <div className="grid grid-cols-2 gap-4">
                    <div className="space-y-1">
                      <span className="text-sm text-muted-foreground">最大组合数:</span>
                      <p className="font-medium">{rule.maxCombinations.toLocaleString()}</p>
                    </div>
                    <div className="space-y-1">
                      <span className="text-sm text-muted-foreground">组合策略:</span>
                      <p className="font-medium">
                        {rule.strategy === "CrossProduct" && "完全叉积"}
                        {rule.strategy === "RandomSampling" && "随机采样"}
                        {rule.strategy === "PrioritySampling" && "优先级采样"}
                      </p>
                    </div>
                  </div>
                </div>
              ))}
            </CardContent>
          </Card>

          {/* 执行约束 */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <Zap className="h-5 w-5 mr-2 text-amber-500" />
                执行约束
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">最大成本限制:</span>
                    <span className="font-medium">${taskData.executionConstraints.maxEstimatedCostUSD}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">最大执行时间:</span>
                    <span className="font-medium">{taskData.executionConstraints.maxExecutionTimeMinutes} 分钟</span>
                  </div>
                </div>
                <div className="space-y-2">
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">最大并发调用数:</span>
                    <span className="font-medium">{taskData.executionConstraints.maxConcurrentAICalls}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-sm text-muted-foreground">批处理大小:</span>
                    <span className="font-medium">{taskData.executionConstraints.batchSize}</span>
                  </div>
                </div>
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="requests" className="space-y-4 pt-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-medium">叉积请求列表</h3>
            <div className="flex items-center gap-2">
              <Badge variant="outline">{crossProductRequests.length} 个请求</Badge>
              <Button variant="outline" size="sm">
                <Download className="mr-2 h-4 w-4" />
                导出结果
              </Button>
            </div>
          </div>

          <div className="rounded-md border">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-[80px]">ID</TableHead>
                  <TableHead>角色</TableHead>
                  <TableHead>场景</TableHead>
                  <TableHead>状态</TableHead>
                  <TableHead>执行时间</TableHead>
                  <TableHead className="text-right">成本</TableHead>
                  <TableHead className="w-[100px]">操作</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {crossProductRequests.map((request) => (
                  <TableRow key={request.id}>
                    <TableCell className="font-medium">{request.id}</TableCell>
                    <TableCell>
                      <div>
                        <div className="font-medium">{request.characterData.name}</div>
                        <div className="text-sm text-muted-foreground">
                          {request.characterData.race} {request.characterData.class} (Lv.{request.characterData.level})
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <div>
                        <div className="font-medium">{request.sceneData.name}</div>
                        <div className="text-sm text-muted-foreground">{request.sceneData.environment}</div>
                      </div>
                    </TableCell>
                    <TableCell>{getRequestStatusBadge(request.status)}</TableCell>
                    <TableCell>
                      {request.executedAt ? (
                        <div className="flex items-center">
                          <Clock className="h-4 w-4 mr-1 text-muted-foreground" />
                          {request.executedAt}
                        </div>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell className="text-right">
                      {request.cost > 0 ? (
                        <div className="flex items-center justify-end">
                          <DollarSign className="h-4 w-4 mr-1 text-green-600" />
                          {request.cost.toFixed(3)}
                        </div>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {request.status === "completed" && (
                        <Button variant="ghost" size="sm" className="h-8 w-8 p-0">
                          <Eye className="h-4 w-4" />
                        </Button>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </TabsContent>

        <TabsContent value="prompts" className="space-y-4 pt-4">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center">
                <FileText className="h-5 w-5 mr-2 text-green-500" />
                Prompt模板
              </CardTitle>
            </CardHeader>
            <CardContent>
              <pre className="whitespace-pre-wrap bg-muted p-4 rounded-md text-sm overflow-auto max-h-80">
                {taskData.promptTemplate.templateContent}
              </pre>
            </CardContent>
          </Card>

          <Card>
            <CardHeader>
              <CardTitle>合成Prompt样例</CardTitle>
              <CardDescription>以下是使用实际数据填充模板后的Prompt样例</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {crossProductRequests.slice(0, 2).map((request) => (
                <Card key={request.id}>
                  <CardHeader className="py-3">
                    <CardTitle className="text-base">
                      {request.characterData.name} × {request.sceneData.name}
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="py-2">
                    <pre className="whitespace-pre-wrap bg-muted p-4 rounded-md text-sm overflow-auto max-h-60">
                      {`你是一个专业的游戏对话编剧。请为以下角色在指定场景中生成一段符合其性格特点的对话。

角色信息：
- 姓名：${request.characterData.name}
- 种族：${request.characterData.race}
- 职业：${request.characterData.class}
- 等级：${request.characterData.level}
- 背景故事：${request.characterData.background}

场景信息：
- 场景名称：${request.sceneData.name}
- 场景描述：${request.sceneData.description}
- 环境类型：${request.sceneData.environment}
- 氛围：${request.sceneData.mood}
- 时间：黄昏

请生成一段3-5句的对话，要求：
1. 符合角色的种族和职业特点
2. 体现角色的等级和经验
3. 适应当前场景的氛围
4. 语言风格要有个性化特色

对话内容：`}
                    </pre>
                  </CardContent>
                </Card>
              ))}
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="results" className="space-y-4 pt-4">
          <div className="flex items-center justify-between">
            <h3 className="text-lg font-medium">生成结果</h3>
            <Button variant="outline" size="sm">
              <Download className="mr-2 h-4 w-4" />
              导出全部结果
            </Button>
          </div>

          <div className="space-y-4">
            {crossProductRequests
              .filter((req) => req.status === "completed" && req.generatedContent)
              .map((request) => (
                <Card key={request.id}>
                  <CardHeader className="py-3">
                    <div className="flex items-center justify-between">
                      <CardTitle className="text-base">
                        {request.characterData.name} 在 {request.sceneData.name}
                      </CardTitle>
                      <div className="flex items-center gap-2">
                        <Badge variant="outline" className="bg-green-50 text-green-700 border-green-200">
                          已完成
                        </Badge>
                        <span className="text-sm text-muted-foreground">${request.cost.toFixed(3)}</span>
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="py-2">
                    <div className="bg-blue-50 p-4 rounded-md border-l-4 border-blue-400">
                      <p className="text-sm italic">"{request.generatedContent}"</p>
                    </div>
                    <div className="mt-2 text-xs text-muted-foreground">生成时间: {request.executedAt}</div>
                  </CardContent>
                </Card>
              ))}
          </div>
        </TabsContent>
      </Tabs>
    </div>
  )
}
