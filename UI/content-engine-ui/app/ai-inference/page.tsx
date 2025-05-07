import { Button } from "@/components/ui/button"
import { PlusCircle } from "lucide-react"
import Link from "next/link"
import InferenceTaskList from "@/components/ai-inference/inference-task-list"

export default function AIInference() {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">AI 推理坊</h1>
          <p className="text-muted-foreground">基于结构化数据进行 AI 分析与内容生成</p>
        </div>
        <Link href="/ai-inference/create">
          <Button>
            <PlusCircle className="mr-2 h-4 w-4" />
            创建推理任务
          </Button>
        </Link>
      </div>

      <InferenceTaskList />
    </div>
  )
}
