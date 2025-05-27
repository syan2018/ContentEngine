import { Button } from "@/components/ui/button"
import { ArrowLeft } from "lucide-react"
import Link from "next/link"
import ReasoningTransactionCreationWizard from "@/components/ai-inference/reasoning-transaction-creation-wizard"

export default function CreateInferenceTask() {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Link href="/ai-inference">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            返回
          </Button>
        </Link>
        <div className="text-muted-foreground">AI 推理坊 / 创建推理任务</div>
      </div>

      <div>
        <h1 className="text-3xl font-bold tracking-tight">创建推理任务</h1>
        <p className="text-muted-foreground">定义一个新的 AI 推理事务，用于从结构化数据生成内容</p>
      </div>

      <ReasoningTransactionCreationWizard />
    </div>
  )
}
