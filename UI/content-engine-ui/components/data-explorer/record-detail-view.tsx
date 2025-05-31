import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"

export default function RecordDetailView({ schemaId, recordId }: { schemaId: string; recordId: string }) {
  // 模拟数据 - 在实际应用中，这些数据会从API获取
  const characterData = {
    id: recordId,
    name: "艾莉娅",
    race: "精灵",
    class: "法师",
    level: 25,
    health: 850,
    mana: 1200,
    strength: 12,
    dexterity: 18,
    intelligence: 24,
    wisdom: 20,
    charisma: 16,
    status: "活跃",
    skills: [
      { name: "火球术", level: 5, damage: "4d6", manaCost: 35 },
      { name: "魔法护盾", level: 3, effect: "减伤30%", manaCost: 45 },
      { name: "闪电链", level: 4, damage: "3d8", manaCost: 50 },
    ],
    equipment: [
      { slot: "头部", name: "智慧之冠", rarity: "稀有", bonus: "+3 智力" },
      { slot: "胸部", name: "秘法长袍", rarity: "精良", bonus: "+15% 法术伤害" },
      { slot: "武器", name: "奥术法杖", rarity: "史诗", bonus: "+25% 法力回复" },
    ],
    background:
      "艾莉娅出生于精灵王国的一个古老家族，从小展现出非凡的魔法天赋。她在精灵魔法学院学习了十年，专攻元素魔法。毕业后，她选择离开家乡，探索世界并寻找失落的魔法知识。",
    createdAt: "2023-04-15 10:30:22",
    updatedAt: "2023-05-02 16:45:37",
  }

  return (
    <div className="space-y-6">
      <Tabs defaultValue="basic" className="space-y-4">
        <TabsList>
          <TabsTrigger value="basic">基本信息</TabsTrigger>
          <TabsTrigger value="attributes">属性</TabsTrigger>
          <TabsTrigger value="skills">技能</TabsTrigger>
          <TabsTrigger value="equipment">装备</TabsTrigger>
          <TabsTrigger value="background">背景故事</TabsTrigger>
          <TabsTrigger value="meta">元数据</TabsTrigger>
        </TabsList>

        <TabsContent value="basic" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>基本信息</CardTitle>
              <CardDescription>角色的基本属性和状态</CardDescription>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">ID</dt>
                  <dd>{characterData.id}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">名称</dt>
                  <dd>{characterData.name}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">种族</dt>
                  <dd>{characterData.race}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">职业</dt>
                  <dd>{characterData.class}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">等级</dt>
                  <dd>{characterData.level}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">状态</dt>
                  <dd>
                    <Badge>{characterData.status}</Badge>
                  </dd>
                </div>
              </dl>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="attributes" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>角色属性</CardTitle>
              <CardDescription>角色的详细属性���值</CardDescription>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">生命值</dt>
                  <dd className="text-2xl font-bold">{characterData.health}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">法力值</dt>
                  <dd className="text-2xl font-bold">{characterData.mana}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">力量</dt>
                  <dd className="text-2xl font-bold">{characterData.strength}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">敏捷</dt>
                  <dd className="text-2xl font-bold">{characterData.dexterity}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">智力</dt>
                  <dd className="text-2xl font-bold">{characterData.intelligence}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">智慧</dt>
                  <dd className="text-2xl font-bold">{characterData.wisdom}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">魅力</dt>
                  <dd className="text-2xl font-bold">{characterData.charisma}</dd>
                </div>
              </dl>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="skills" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>技能列表</CardTitle>
              <CardDescription>角色已掌握的技能</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {characterData.skills.map((skill, index) => (
                  <Card key={index}>
                    <CardHeader className="py-3">
                      <div className="flex items-center justify-between">
                        <CardTitle className="text-lg">{skill.name}</CardTitle>
                        <Badge variant="outline">等级 {skill.level}</Badge>
                      </div>
                    </CardHeader>
                    <CardContent className="py-2">
                      <dl className="grid grid-cols-1 md:grid-cols-2 gap-2">
                        {skill.damage && (
                          <div className="space-y-1">
                            <dt className="text-sm font-medium text-muted-foreground">伤害</dt>
                            <dd>{skill.damage}</dd>
                          </div>
                        )}
                        {skill.effect && (
                          <div className="space-y-1">
                            <dt className="text-sm font-medium text-muted-foreground">效果</dt>
                            <dd>{skill.effect}</dd>
                          </div>
                        )}
                        <div className="space-y-1">
                          <dt className="text-sm font-medium text-muted-foreground">法力消耗</dt>
                          <dd>{skill.manaCost}</dd>
                        </div>
                      </dl>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="equipment" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>装备列表</CardTitle>
              <CardDescription>角色当前装备的物品</CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {characterData.equipment.map((item, index) => (
                  <div key={index} className="flex items-center p-3 border rounded-md">
                    <div className="flex-1">
                      <div className="font-medium">{item.name}</div>
                      <div className="text-sm text-muted-foreground">{item.slot}</div>
                    </div>
                    <div className="flex items-center gap-2">
                      <Badge variant={item.rarity === "史诗" ? "default" : "secondary"}>{item.rarity}</Badge>
                      <div className="text-sm font-medium">{item.bonus}</div>
                    </div>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="background" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>背景故事</CardTitle>
              <CardDescription>角色的历史和背景</CardDescription>
            </CardHeader>
            <CardContent>
              <p className="whitespace-pre-line">{characterData.background}</p>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="meta" className="space-y-4">
          <Card>
            <CardHeader>
              <CardTitle>元数据</CardTitle>
              <CardDescription>记录的系统信息</CardDescription>
            </CardHeader>
            <CardContent>
              <dl className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">创建时间</dt>
                  <dd>{characterData.createdAt}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">最后更新</dt>
                  <dd>{characterData.updatedAt}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">记录ID</dt>
                  <dd>{characterData.id}</dd>
                </div>
                <div className="space-y-1">
                  <dt className="text-sm font-medium text-muted-foreground">数据结构</dt>
                  <dd>游戏角色</dd>
                </div>
              </dl>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  )
}
