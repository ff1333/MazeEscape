# 迷宫逃生完整开发解释

这是一份持续更新的学习文档，用来解释《迷宫逃生》从主菜单、程序化迷宫、玩家控制、武器战斗到僵尸行为和关卡结算的完整开发过程。

当前主体基于正式版 `v1.3.1`，并将从 `v1.4` 开始持续记录敌人感知、记忆、群体警报和 Utility AI 的改造过程。

## 一、先用一句话理解游戏

《迷宫逃生》是一款 3D 生存迷宫游戏：

```text
进入开始界面
→ 选择已解锁关卡
→ 选择冲锋枪或长刀
→ 从迷宫外进入入口
→ 探索程序化迷宫
→ 对抗普通和精英僵尸
→ 应对浓雾、昼夜和瘴气
→ 找到终点门
→ 解锁下一关
```

项目重点展示：

- 3D 角色控制和第一/第三人称相机；
- 程序化迷宫生成；
- A* 寻路；
- 敌人状态机；
- 枪械和近战；
- 对象池；
- 小地图探索；
- 十关数据配置和本地存档；
- 环境机制；
- 自动测试和多平台交付。

## 二、项目总体结构

| 系统 | 主要脚本 | 负责什么 |
|---|---|---|
| 游戏流程 | `GameDirector`、`GameTypes`、`CampaignProgress` | 页面阶段、选关、开始、胜负和存档 |
| 关卡数据 | `LevelDefinition`、`LevelCatalog` | 十关尺寸、种子、敌人和环境参数 |
| 迷宫 | `MazeGenerator`、`MazeLayout`、`MazeWorld` | 生成逻辑网格并构建三维场景 |
| 寻路 | `MazePathfinder` | 在逻辑网格上执行 A* |
| 玩家 | `GameInput`、`PlayerController`、`CameraRig` | 输入、移动、视角、冲刺和角色状态 |
| 战斗 | `WeaponController`、`Health`、`CombatEffects` | 枪械、近战、伤害、装弹和反馈 |
| 敌人 | `EnemyPool`、`EnemyAgent`、`SpawnTelegraph` | 生成、复用、移动、攻击和死亡 |
| 环境 | `EnvironmentController`、`SafeLightLayout` | 浓雾、昼夜、瘴气和安全灯 |
| UI | `GameHud`、`MiniMapHud`、`MobileControls` | 菜单、HUD、小地图和移动端操作 |

总体关系：

```text
GameDirector 管理当前关卡和游戏阶段
├─ MazeWorld 根据 LevelDefinition 构建迷宫
├─ PlayerController 读取 GameInput 并移动
├─ WeaponController 执行枪械或长刀攻击
├─ EnemyPool 生成 EnemyAgent
├─ EnemyAgent 使用 MazeWorld.FindPath 追击
├─ EnvironmentController 应用关卡环境
└─ GameHud 根据 GameDirector 和玩家状态显示界面
```

## 三、GameDirector 为什么是核心入口

`GameDirector` 管理当前 `GamePhase`：

```text
Title
Guide
LevelSelect
Staging
Playing
Paused
Won
Lost
```

这些阶段比多个互不相关的布尔值更清晰。例如不需要同时维护：

```text
isTitle
isSelectingLevel
isPlaying
isPaused
isDead
```

当前阶段决定：

- 玩家能否移动和攻击；
- 鼠标是否锁定；
- 显示哪个 UI；
- 是否刷怪；
- 是否计算环境伤害；
- 是否可以进入下一关。

## 四、从打开游戏到进入第一关

### 4.1 开始界面

游戏启动后进入 `Title`。HUD 显示开始、图鉴、设置和退出等入口，玩家暂时不能控制角色。

点击开始后：

```text
GameDirector.ShowLevelSelection
→ Phase = LevelSelect
→ GameHud 显示十关选择
```

### 4.2 关卡为什么会锁定

`CampaignProgress` 使用 `PlayerPrefs` 保存已完成关卡和已解锁关卡。

玩家只有完成前一关，才能选择下一关。读取存档后，关卡按钮根据解锁结果设置 `interactable`。

这属于轻量本地存档，适合作品集单机游戏；它不具备云存档、防作弊和多用户功能。

### 4.3 选择关卡

选择关卡后，`GameDirector` 从 `LevelCatalog` 获取对应 `LevelDefinition`，其中包含：

- 关卡编号；
- 迷宫宽高；
- 固定随机种子；
- 敌人数量和生成节奏；
- 是否有浓雾；
- 是否有昼夜变化；
- 是否有瘴气；
- 是否有精英敌人。

随后调用 `MazeWorld.Build(level)` 构建该关卡，并进入 `Staging` 武器选择阶段。

### 4.4 选择武器

玩家选择冲锋枪或长刀后，`WeaponController` 切换武器类型和模型。角色仍位于迷宫外的准备区，穿过入口触发器后才正式进入 `Playing`。

这样把“选完武器”和“开始计时刷怪”分开，玩家可以先观察操作说明和入口方向。

## 五、十关为什么每关迷宫不同

### 5.1 固定种子

每关使用不同固定种子。固定种子的意义是：

- 不同关卡布局不同；
- 同一关重新打开时可以复现；
- 测试失败时能定位到相同布局；
- 演示和面试时结果相对稳定。

随机不等于每次都必须不同。工程调试更需要可复现。

### 5.2 Growing Tree 算法

`MazeGenerator` 创建二维 `MazeLayout`，每个格子记录四个方向是否连通。

生成过程：

1. 从起点格开始；
2. 维护已经激活的前沿格子列表；
3. 选择一个前沿格；
4. 找一个未访问邻居；
5. 打通当前格和邻居之间的墙；
6. 将邻居加入前沿；
7. 没有未访问邻居时回退。

当前实现以一定概率继续从列表末尾扩展，否则从旧前沿随机选择。连续从末尾扩展容易形成长通道，从旧位置扩展会增加分岔。

### 5.3 候选筛选

项目不会只生成一次就接受，而是根据关卡要求评价候选布局，例如：

- 起点到终点的路线长度；
- 转弯数量；
- 分岔密度；
- 早期是否出现选择；
- 是否存在足够的支路和死路。

因此后期关卡不仅尺寸更大，也可以在结构上更复杂。

## 六、逻辑迷宫如何变成三维场景

`MazeWorld.Build()` 先调用生成器获得逻辑网格，然后遍历所有格子：

- 创建地面；
- 根据格子开放方向决定是否创建墙；
- 创建墙体装饰和支撑；
- 创建入口准备区；
- 创建开始门和入口触发器；
- 创建终点区域和出口触发器；
- 根据关卡生成避难灯；
- 最后进行静态批处理。

`CellToWorld()` 把二维格子坐标转换为三维世界坐标；`WorldToCell()` 做反向转换。

敌人寻路使用格子坐标，角色和画面使用世界坐标，这两个转换函数把逻辑层和表现层连接起来。

## 七、入口和终点为什么使用 Trigger

入口和终点都是触发区域：

```text
PlayerController 进入 Entry Trigger
→ MazeEntryTrigger.OnTriggerEnter
→ GameDirector.BeginRun
```

```text
PlayerController 进入 Exit Trigger
→ MazeExitTrigger.OnTriggerEnter
→ GameDirector.CompleteLevel
```

Trigger 只负责检测进入，不作为实体墙阻挡角色，因此适合关卡入口、出口、拾取物和范围检测。

开始门的碰撞和显示逻辑与入口触发器分开，避免早期出现“看得到入口但进不去”或“从侧面掉出地图”的问题。

## 八、玩家移动和镜头如何工作

### 8.1 GameInput

`GameInput` 统一读取：

- 移动；
- 鼠标视角；
- 攻击；
- 冲刺；
- 装弹；
- 第一/第三人称切换；
- 暂停；
- 移动端按钮状态。

玩家控制器不直接分别判断键盘、鼠标和触屏，而是读取 `GameInput` 整理后的状态。

### 8.2 PlayerController

角色使用 `CharacterController` 移动。主要流程：

```text
读取移动输入
→ 根据相机朝向转换为世界方向
→ 应用走路或冲刺速度
→ CharacterController.Move
→ 更新重力和地面状态
→ 调用 WeaponController
```

相比直接修改 `transform.position`，`CharacterController` 更适合可控的第一/第三人称角色移动和碰撞滑动。

### 8.3 CameraRig

镜头保存水平和垂直旋转角度。鼠标或触摸改变视角，垂直角度被限制，避免镜头完全翻转。

第一人称与第三人称共享玩家位置和战斗逻辑，只改变镜头位置、角色显示和瞄准方式。

按 `Esc` 暂停时释放鼠标，继续游戏时重新锁定，解决鼠标无法退出游戏画面的问题。

## 九、冲锋枪如何攻击

冲锋枪流程：

```text
按住攻击
→ 检查射击间隔
→ 检查弹匣和装弹状态
→ 从相机中心发射射线确定瞄准点
→ 从枪口向瞄准点再次检查真实遮挡
→ 命中 EnemyAgent 时造成伤害
→ 播放枪口、命中、后坐力和声音反馈
→ 子弹减少
```

为什么需要两次检测？

第三人称相机不在枪口位置。如果只从相机发射射线，可能出现相机看得到敌人，但枪口前面有墙，子弹仍然穿墙命中的问题。

装弹期间不能射击。弹匣补满后播放完成提示音并显示短暂 READY，让玩家知道脆弱窗口已经结束。

## 十、长刀如何攻击

长刀不是简单检测一个固定圆形范围，而是结合：

- 攻击距离；
- 面向夹角；
- 墙壁遮挡；
- 攻击冷却。

只有位于有效距离和前方角度内、且没有被墙挡住的敌人才受到伤害。

攻击距离经过调整后比早期小刀更长，但仍然要求玩家主动靠近并承担风险。

## 十一、敌人当前如何行动

### 11.1 EnemyPool

对象池按需要创建僵尸，关卡开始时可以预热一定数量。

生成时：

```text
寻找未激活 EnemyAgent
→ 没有则创建
→ EnemyAgent.Spawn
→ 设置目标、世界、位置和精英状态
→ 重置生命、路径、攻击时间和模型
```

死亡后播放动画，等待约 1.1 秒，再停用对象返回池中。

### 11.2 当前 EnemyAgent

正式版当前使用：

```text
Wander
Chase
Attack
Dead
Inactive
```

更新流程：

1. 如果未激活、死亡、没有玩家或不在 Playing 阶段，停止更新；
2. 计算玩家距离；
3. 近距离进入攻击；
4. 32 米内进入追击，否则游荡；
5. 定期调用 `MazeWorld.FindPath()`；
6. 沿路径格子移动；
7. 攻击时再次使用 Linecast 检查遮挡。

当前不足是它只根据距离发现玩家，隔墙和浓雾不会真正影响“是否发现”，也没有最后已知位置和搜索行为。

### 11.3 v1.4 改造方向

状态将扩展为：

```text
Patrol
Investigate
Chase
Search
Attack
```

后续增加视觉、听觉、记忆、群体警报和 Utility AI。详细步骤持续追加到本文件。

## 十二、A* 寻路如何工作

`MazePathfinder` 不直接在三维碰撞体中搜索，而是在已经生成好的 `MazeLayout` 上搜索。

主要集合：

- `open`：还需要检查的格子；
- `closed`：已经处理的格子；
- `parent`：每个格子从哪里到达；
- `cost`：从起点到该格子的实际代价。

评分：

```text
实际代价 g
+ 到终点的曼哈顿距离 h
= 估计总代价 f
```

找到终点后通过 `parent` 反向重建路径，再翻转为从起点到终点的顺序。

复用迷宫逻辑网格的好处是每次生成迷宫后不需要重新烘焙 NavMesh，也容易在 EditMode 中测试路径是否存在。

## 十三、敌人为什么不会直接出现在玩家脸上

关卡导演先选择合法迷宫格，再创建 `SpawnTelegraph` 预警。经过短暂延迟后，从 `EnemyPool` 获取敌人。

预警给予玩家反应时间，也让“敌人突然凭空出现”变成可读的玩法信号。

生成规则需要考虑：

- 与玩家距离；
- 是否在迷宫有效区域；
- 当前敌人上限；
- 普通或精英敌人；
- 当前关卡难度。

## 十四、小地图如何帮助探索

`MazeExploration` 记录玩家已经走过的格子，`MiniMapHud` 根据记录绘制：

- 已探索区域；
- 玩家位置和朝向；
- 必要的入口或状态信息。

小地图不直接显示完整出口路线，因此它减少迷路挫败，但不取消迷宫探索。

## 十五、环境机制如何增加难度

### 浓雾

限制远距离观察。目前主要改变画面表现；v1.4 视觉感知会让浓雾同时缩短僵尸的有效视觉距离。

### 昼夜

改变环境光照和可见性，让同一套迷宫在不同时段有不同氛围和判断压力。

### 瘴气

玩家离开安全灯范围后持续受到伤害，需要在探索路线和安全范围之间做取舍。

### 安全灯

`SafeLightLayout` 在主路、支路和死路上布局灯柱，不保证灯光方向就是正确出口，避免灯柱变成导航箭头。

## 十六、生命、胜负和关卡解锁

`Health` 负责当前生命、最大生命、扣血、恢复和死亡事件。

玩家死亡：

```text
Health.Died
→ GameDirector 进入 Lost
→ 停止角色、敌人和环境更新
→ 释放鼠标
→ GameHud 显示失败界面
```

到达终点：

```text
MazeExitTrigger
→ GameDirector.CompleteLevel
→ CampaignProgress.Complete
→ 保存解锁进度
→ 进入 Won
→ 显示下一关或返回选关
```

## 十七、UI、设置和本地化

`GameHud` 使用 partial class 分拆主 HUD、关卡菜单和图鉴代码，避免单文件继续膨胀。

`PortfolioSettings` 使用 `PlayerPrefs` 保存语言和音量设置，`LocalizedLabel` 负责中英文文本刷新。

设置菜单不会把音量滑块清零；关闭声音只改变静音状态，重新开启后恢复原音量。

## 十八、测试为什么分 EditMode 和 PlayMode

### EditMode `15/15`

适合验证不需要真实运行场景的纯逻辑：

- 迷宫一定连通；
- 不同种子结果不同；
- 路线长度和复杂度；
- 安全灯布局；
- A* 路径结果。

### PlayMode `35/35`

适合验证需要场景和组件生命周期的行为：

- 开始页、选关和武器选择；
- 角色和入口碰撞；
- 枪械、长刀和装弹；
- 敌人对象池复用；
- 设置保存；
- 胜负和关卡解锁。

自动测试用于防止版本迭代破坏规则，不能代替真实玩家对难度和手感的判断。

## 十九、完整调用链示例

### 示例一 选择关卡并进入迷宫

```text
开始按钮
→ GameDirector.ShowLevelSelection
→ 选择关卡
→ LevelCatalog 获取 LevelDefinition
→ MazeWorld.Build
→ MazeGenerator 生成 MazeLayout
→ MazeWorld 创建地面、墙、入口、出口和灯
→ 选择武器
→ 进入 Entry Trigger
→ GameDirector.BeginRun
```

### 示例二 僵尸追击玩家

```text
关卡刷怪逻辑
→ SpawnTelegraph
→ EnemyPool.Spawn
→ EnemyAgent.Spawn
→ EnemyAgent.Update
→ MazeWorld.FindPath
→ MazePathfinder.FindPath
→ CharacterController.SimpleMove
→ 进入攻击距离
→ Linecast 检查遮挡
→ Player.Health.Damage
```

### 示例三 玩家用冲锋枪击杀僵尸

```text
GameInput.AttackHeld
→ PlayerController 调用 WeaponController.Tick
→ 检查弹匣、装弹和冷却
→ 相机射线确定目标点
→ 枪口射线检查遮挡
→ EnemyAgent.TakeDamage
→ Health.Died
→ EnemyAgent.OnDied
→ GameDirector.RegisterKill
→ 播放死亡动画并回收到 EnemyPool
```

## 二十、面试时怎样介绍架构

可以先说：

> 项目由 GameDirector 管理标题、选关、准备、战斗和胜负阶段。关卡使用 LevelDefinition 配置，MazeGenerator 生成逻辑网格，MazeWorld 把网格构建成三维场景，敌人与出口寻路复用同一份逻辑网格。玩家输入、相机、武器、敌人和 UI 分成独立组件，高频僵尸使用对象池复用。

然后根据面试官追问深入某一条调用链，不要一开始把所有算法名堆出来。

## 二十一、项目当前不足

- 敌人发现玩家主要依赖距离；
- 没有视觉锥、听觉和目标记忆；
- 敌人之间没有有限通信；
- 敌人行为规则集中在 `EnemyAgent`；
- 没有联网、热更新和商业存档；
- 真机测试次数和设备覆盖有限。

这些不足不是隐藏项，而是 v1.4 敌人 AI 改造的出发点。

## 二十二、v1.4 更新记录入口

### 第一步 状态骨架

目标：把旧的 `Wander / Chase / Attack` 扩展为语义明确的 `Patrol / Investigate / Chase / Search / Attack`，统一通过 `SetState` 切换，并保持现有玩法不变。

详细教程：[`V1_4_STEP_01_ENEMY_STATE_SKELETON.md`](V1_4_STEP_01_ENEMY_STATE_SKELETON.md)

后续每完成一个步骤，都在本节增加：

- 需求；
- 玩家可见变化；
- 参考的官方或开源方案；
- 修改的脚本；
- 调用链；
- 测试结果；
- 已知限制；
- 面试讲法。

## 二十三、以后如何使用这份文档

第一次阅读不需要背代码。按顺序做到：

1. 能说出游戏主循环；
2. 能画出 GameDirector、MazeWorld、PlayerController、EnemyAgent 的关系；
3. 能解释一个完整调用链；
4. 能在代码中找到对应方法；
5. 能提出不理解的具体章节；
6. 能完成一个小需求修改或排错。

能够解释和修改比重复背诵整份文档更重要。
