"use client"

import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Textarea } from "@/components/ui/textarea"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import type { ReasoningTransactionDefinition } from "../reasoning-transaction-creation-wizard"

interface BasicInfoStepProps {
  transactionDefinition: ReasoningTransactionDefinition
  updateTransactionDefinition: (updates: Partial<ReasoningTransactionDefinition>) => void
  availableSchemas: { name: string; displayName: string; fieldCount: number }[]
}

export default function BasicInfoStep({
  transactionDefinition,
  updateTransactionDefinition,
  availableSchemas,
}: BasicInfoStepProps) {
  return (
    <div className="space-y-6">
      <div className="space-y-2">
        <Label htmlFor="transaction-name">推理事务名称</Label>
        <Input
          id="transaction-name"
          placeholder="例如：NPC对话生成"
          value={transactionDefinition.name}
          onChange={(e) => updateTransactionDefinition({ name: e.target.value })}
        />
      </div>

      <div className="space-y-2">
        <Label htmlFor="transaction-description">描述</Label>
        <Textarea
          id="transaction-description"
          placeholder="描述这个推理事务的用途和预期输出..."
          rows={4}
          value={transactionDefinition.description}
          onChange={(e) => updateTransactionDefinition({ description: e.target.value })}
        />
      </div>
    </div>
  )
}
