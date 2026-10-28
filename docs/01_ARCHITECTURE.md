# 架构说明

## 启动流程

`Main.unity` 只保存一个 `GameDirector`。运行后按以下顺序创建系统：

1. 创建输入、迷宫、敌人池和环境控制器。
2. 创建玩家、双视角相机和武器控制器。
3. 创建 HUD 与移动端触控层。
4. 读取 `CampaignProgress`，用第一关作为开始页的场景背景，进入 `Title`。
5. 点击 START 进入 `LevelSelect`，只有已解锁关卡允许进入。
6. 选择关卡后生成该关迷宫、预热对象池，进入双武器确认阶段 `Staging`。

运行时生成使十关共享同一份代码和场景，关卡差异集中在
`LevelDefinition`，避免复制场景后参数逐渐失控。

## 主要模块

| 模块 | 关键脚本 | 职责 |
| --- | --- | --- |
| 流程 | `GameDirector` | 阶段、关卡、存档、生成、胜负 |
| 进度 | `CampaignProgress` | 通关位标记、顺序解锁、旧存档迁移 |
| 模型导入 | `FogboundCharacterBuilder` | 配置 FBX 动画、贴图及可复用预制体 |
| 角色动画 | `PlayerVisual`、`EnemyAgent` | 播放待机、移动、攻击和死亡片段 |
| 迷宫 | `MazeGenerator` | 固定种子的 Growing Tree 混合生长与候选筛选 |
| 难度 | `MazeComplexity` | 路线、转弯、死路、全图路口及主路线选择间距 |
| 寻路 | `MazePathfinder` | 网格 A* 与路径重建 |
| 世界 | `MazeWorld` | 生成碰撞体、门、灯和坐标转换 |
| 玩家 | `PlayerController` | CharacterController 移动与重力 |
| 相机 | `CameraRig` | 第一/第三人称切换和墙体避让 |
| 战斗 | `WeaponController` | 双武器切换、Hitscan 冲锋枪、手动换弹、近战检测 |
| AI | `EnemyPerception`、`EnemyDecisionModel`、`EnemyAgent` | 感知、记忆、Utility 评分与行为执行 |
| 性能 | `EnemyPool` | 按关卡上限预热并复用敌人 |
| 环境 | `EnvironmentController`、`MazeWorld` | 固定地下城状态、局部雾区、照明与污染 |
| 输入 | `GameInput` | Input System 与触控输入统一 |
| UI | `GameHud`、`GameHud.Campaign` | 开始页、选关、双武器 HUD、准备、暂停与结算 |
| 探索地图 | `MazeExploration`、`MiniMapHud` | 记录已走格子，以小纹理显示路径、朝向和未探索岔路口 |

## 为什么不用 NavMesh

迷宫在运行时生成。使用 NavMesh 需要运行时重新烘焙，并引入额外平台与包依赖。
敌人直接使用迷宫的逻辑网格做 A*，路径天然不会穿墙，算法也可以在 EditMode
中独立测试。当前上限只有 14 个敌人，每个敌人按 0.45-0.7 秒间隔重算路径，
不需要每帧寻路。

## 第一/第三人称

两种视角共用同一个 `CameraRig`、准星和攻击方向。第三人称使用球形检测缩短
相机距离，避免墙体挡住镜头；第一人称把相机放在眼位。角色移动始终根据相机
水平朝向计算，因此切换视角不会改变操作语义。

## 数据流

选关入口在按钮和 `GameDirector.SelectLevel` 两层检查解锁状态，不能只靠灰色按钮。
只有 `Win()` 才调用 `CampaignProgress.Complete()` 写入 PlayerPrefs 并 Save。
保存值是十关完成位标记，只接受从第一关开始的连续完成记录。旧版真正的解锁值可迁移，
但 `Fogbound.SelectedLevel` 不作为通关证明。PlayerPrefs 是本地便捷存档，不提供防篡改或云同步。
返回菜单时清理敌人、触控残留、装备和暂停状态；生成代数阻止旧关卡预警回调在新关刷怪。

## 角色与动画

使用 Quaternius CC0 模型、贴图和作者提供的动画。`FogboundCharacterBuilder` 生成
Resources 内的角色及武器预制体，实际版本使用 Legacy `Animation.CrossFade` 播放片段，
没有使用 Animator 状态机或根运动。移动仍由 CharacterController 控制。
普通/精英外观随对象池一起预热，复用时重置碰撞体、攻击计时、生命、外观与动画。
受击闪白用 MaterialPropertyBlock，结束时清空覆盖值，恢复原贴图颜色。
不能把第三方建模或动画创作写成自己的工作；本项目工作是导入、绑定、运行时切换和复用集成。

## 游戏内数据流

输入 -> 玩家/相机 -> 武器命中或移动 -> Health/AI -> GameDirector -> HUD。

系统之间优先通过公开状态和事件通信，例如 `Health.Died`，而不是在多个脚本里
重复查询场景对象。

HUD 订阅 `Health.Changed`，扣血、治疗与重置时立即更新文字和血条宽度，并在销毁时退订。
纯色 Image 没有 Sprite 时不能依赖 Filled 的填充裁切，血条通过 RectTransform 的横向锚点比例控制宽度。
胜负结算前还会刷新顶栏，避免阶段切换后停止 Update 导致最后一帧数据滞后。

## 探索小地图

`MazeExploration` 只保存玩家真正走过的格子，起点默认已知，终点与未走区域不会提前揭晓。
已走格子通往未走区域的开口显示金色短标记，只展示路口，不展示邻格中心或更远的路径。
`MiniMapHud` 只在进入新格子时重绘小纹理，而不是每帧重建数百个 UI 元素；当前位置与
相机朝向的标记单独更新。每关加载时创建新的探索记录，避免把上一关的信息带进来。
