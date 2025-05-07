"use client"

import Link from "next/link"
import { usePathname } from "next/navigation"
import { cn } from "@/lib/utils"
import { BarChart3, Database, FileInput, Search, Sparkles, Settings, HelpCircle, Menu, X } from "lucide-react"
import { Button } from "@/components/ui/button"
import { useState } from "react"

const navigation = [
  { name: "首页", href: "/", icon: BarChart3 },
  { name: "数据管理", href: "/schema-management", icon: Database },
  { name: "信息注入", href: "/data-entry", icon: FileInput },
  { name: "数据洞察", href: "/data-explorer", icon: Search },
  { name: "AI 推理坊", href: "/ai-inference", icon: Sparkles },
  { name: "引擎配置", href: "/settings", icon: Settings },
  { name: "帮助中心", href: "/help", icon: HelpCircle },
]

export default function Sidebar() {
  const pathname = usePathname()
  const [sidebarOpen, setSidebarOpen] = useState(false)

  return (
    <>
      <Button
        variant="outline"
        size="icon"
        className="fixed left-4 top-4 z-40 md:hidden"
        onClick={() => setSidebarOpen(true)}
      >
        <Menu className="h-4 w-4" />
      </Button>

      <div
        className={cn(
          "fixed inset-0 z-50 bg-background/80 backdrop-blur-sm md:hidden",
          sidebarOpen ? "block" : "hidden",
        )}
        onClick={() => setSidebarOpen(false)}
      />

      <div
        className={cn(
          "fixed inset-y-0 left-0 z-50 w-64 bg-white dark:bg-gray-950 shadow-lg transform transition-transform duration-200 ease-in-out md:translate-x-0 md:shadow-none md:relative",
          sidebarOpen ? "translate-x-0" : "-translate-x-full",
        )}
      >
        <div className="flex h-16 items-center justify-between px-4 border-b">
          <Link href="/" className="flex items-center space-x-2">
            <Sparkles className="h-6 w-6 text-purple-600" />
            <span className="font-bold text-xl">ContentEngine</span>
          </Link>
          <Button variant="ghost" size="icon" className="md:hidden" onClick={() => setSidebarOpen(false)}>
            <X className="h-4 w-4" />
          </Button>
        </div>
        <nav className="flex flex-col gap-1 p-4">
          {navigation.map((item) => {
            const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`)
            return (
              <Link
                key={item.name}
                href={item.href}
                className={cn(
                  "flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors",
                  isActive
                    ? "bg-purple-100 text-purple-900 dark:bg-purple-900/20 dark:text-purple-50"
                    : "text-gray-700 hover:bg-gray-100 dark:text-gray-300 dark:hover:bg-gray-800",
                )}
              >
                <item.icon className={cn("h-5 w-5", isActive ? "text-purple-600" : "text-gray-400")} />
                {item.name}
              </Link>
            )
          })}
        </nav>
      </div>
    </>
  )
}
