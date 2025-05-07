"use client"

import { useState } from "react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Loader2, CheckCircle2 } from "lucide-react"

export default function AIServiceSettings() {
  const [isTesting, setIsTesting] = useState(false)
  const [testSuccess, setTestSuccess] = useState<boolean | null>(null)

  const testConnection = () => {
    setIsTesting(true)
    // 模拟 API 测试
    setTimeout(() => {
      setTestSuccess(true)
      setIsTesting(false)
    }, 2000)
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>AI 服务配置</CardTitle>
        <CardDescription>配置连接到大语言模型所需的信息</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="ai-provider">AI 服务提供商</Label>
          <Select defaultValue="openai">
            <SelectTrigger id="ai-provider">
              <SelectValue placeholder="选择 AI 服务提供商" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="openai">OpenAI</SelectItem>
              <SelectItem value="azure">Azure OpenAI</SelectItem>
              <SelectItem value="anthropic">Anthropic</SelectItem>
              <SelectItem value="local">本地模型</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="api-key">API 密钥</Label>
          <Input
            id="api-key"
            type="password"
            placeholder="输入您的 API 密钥"
            defaultValue="sk-••••••••••••••••••••••••••••••"
          />
        </div>
        <div className="space-y-2">
          <Label htmlFor="model">默认模型</Label>
          <Select defaultValue="gpt-4">
            <SelectTrigger id="model">
              <SelectValue placeholder="选择默认模型" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="gpt-4">GPT-4</SelectItem>
              <SelectItem value="gpt-4o">GPT-4o</SelectItem>
              <SelectItem value="gpt-3.5-turbo">GPT-3.5 Turbo</SelectItem>
              <SelectItem value="claude-3-opus">Claude 3 Opus</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="api-endpoint">API 端点 (可选)</Label>
          <Input id="api-endpoint" placeholder="https://api.openai.com/v1" defaultValue="https://api.openai.com/v1" />
        </div>
      </CardContent>
      <CardFooter className="flex justify-between">
        <div className="flex items-center">
          {testSuccess === true && (
            <div className="flex items-center text-green-600">
              <CheckCircle2 className="mr-1 h-4 w-4" />
              <span>连接成功</span>
            </div>
          )}
        </div>
        <div className="flex space-x-2">
          <Button variant="outline" onClick={testConnection} disabled={isTesting}>
            {isTesting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                测试中...
              </>
            ) : (
              "测试连接"
            )}
          </Button>
          <Button>保存配置</Button>
        </div>
      </CardFooter>
    </Card>
  )
}
