import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from "@/components/ui/card"
import { Switch } from "@/components/ui/switch"

export default function SystemSettings() {
  return (
    <Card>
      <CardHeader>
        <CardTitle>系统参数设置</CardTitle>
        <CardDescription>配置应用的基本参数和行为</CardDescription>
      </CardHeader>
      <CardContent className="space-y-4">
        <div className="space-y-2">
          <Label htmlFor="page-size">默认分页大小</Label>
          <Select defaultValue="20">
            <SelectTrigger id="page-size">
              <SelectValue placeholder="选择默认分页大小" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="10">10 条/页</SelectItem>
              <SelectItem value="20">20 条/页</SelectItem>
              <SelectItem value="50">50 条/页</SelectItem>
              <SelectItem value="100">100 条/页</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="date-format">日期时间格式</Label>
          <Select defaultValue="yyyy-MM-dd HH:mm:ss">
            <SelectTrigger id="date-format">
              <SelectValue placeholder="选择日期时间格式" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="yyyy-MM-dd HH:mm:ss">YYYY-MM-DD HH:MM:SS</SelectItem>
              <SelectItem value="yyyy/MM/dd HH:mm">YYYY/MM/DD HH:MM</SelectItem>
              <SelectItem value="dd/MM/yyyy HH:mm">DD/MM/YYYY HH:MM</SelectItem>
              <SelectItem value="MM/dd/yyyy HH:mm">MM/DD/YYYY HH:MM</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-2">
          <Label htmlFor="data-path">数据存储路径</Label>
          <Input id="data-path" placeholder="数据文件存储路径" defaultValue="./data" />
        </div>
        <div className="flex items-center space-x-2">
          <Switch id="auto-backup" defaultChecked />
          <Label htmlFor="auto-backup">启用自动备份</Label>
        </div>
        <div className="flex items-center space-x-2">
          <Switch id="debug-mode" />
          <Label htmlFor="debug-mode">调试模式</Label>
        </div>
      </CardContent>
      <CardFooter className="flex justify-end">
        <Button>保存设置</Button>
      </CardFooter>
    </Card>
  )
}
