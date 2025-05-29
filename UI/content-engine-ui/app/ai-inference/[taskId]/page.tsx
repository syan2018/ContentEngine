import { Button } from "@/components/ui/button"
import { ArrowLeft } from "lucide-react"
import Link from "next/link"
import InferenceTaskDetail from "@/components/ai-inference/inference-task-detail"

export default function InferenceTaskDetailPage({ params }: { params: { taskId: string } }) {
  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Link href="/ai-inference">
          <Button variant="ghost" size="sm">
            <ArrowLeft className="mr-2 h-4 w-4" />
            返回推理任务列表
          </Button>
        </Link>
        <div className="text-muted-foreground">AI 推理坊 / 任务详情</div>
      </div>

      <InferenceTaskDetail taskId={params.taskId} />
    </div>
  )
}
