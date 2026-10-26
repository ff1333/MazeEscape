# 迷宫逃生 v1.4 第一步 敌人状态骨架

本步骤只重构敌人状态的表达方式，不增加视觉、听觉、记忆或 Utility AI。完成后游戏表现应与当前版本基本一致，但代码已经为后续的调查和搜索行为预留清晰状态。

## 一、本步骤完成后会得到什么

敌人状态从：

```text
Inactive → Wander → Chase → Attack → Dead
```

扩展为：

```text
Inactive
Patrol
Investigate
Chase
Search
Attack
Dead
```

本步骤中真正运行的仍然只有：

```text
Patrol
Chase
Attack
Dead
Inactive
```

`Investigate` 和 `Search` 先定义但暂不进入。下一步加入视觉与声音感知后再使用它们。

## 二、开始前检查分支

### 2.1 打开 PowerShell

在 Windows 开始菜单中搜索并打开 PowerShell，然后执行：

```powershell
Set-Location -LiteralPath "D:\software\documents\unity_games\projects\learning\MazeEscape"
git status
```

正确结果应包含：

```text
On branch feature/intelligent-enemy-ai
nothing to commit, working tree clean
```

如果分支不是 `feature/intelligent-enemy-ai`，停止修改并执行：

```powershell
git switch feature/intelligent-enemy-ai
```

再次执行 `git status`，确认分支和工作区正确。

## 三、修改敌人状态枚举

### 3.1 打开文件

在 Visual Studio、Rider 或 VS Code 中打开：

```text
Assets/Scripts/Core/GameTypes.cs
```

找到原来的代码：

```csharp
public enum EnemyState
{
    Inactive,
    Wander,
    Chase,
    Attack,
    Dead
}
```

替换为：

```csharp
public enum EnemyState
{
    Inactive,
    Patrol,
    Investigate,
    Chase,
    Search,
    Attack,
    Dead
}
```

保存文件。

### 3.2 为什么要这样修改

- `Patrol` 表示没有发现玩家时的正常巡逻，比 `Wander` 更明确。
- `Investigate` 表示听到声音或收到同伴警报后前往可疑位置。
- `Chase` 表示当前确认玩家位置并追击。
- `Search` 表示失去玩家后在最后已知位置附近搜索。
- `Attack` 表示进入攻击距离且攻击条件成立。
- `Inactive` 和 `Dead` 分别表示对象池停用状态和死亡等待回收状态。

现在不为 `Investigate` 和 `Search` 编写假逻辑。状态先存在，后续功能再逐步接入。

## 四、给 EnemyAgent 增加统一状态切换入口

### 4.1 打开文件

打开：

```text
Assets/Scripts/AI/EnemyAgent.cs
```

找到：

```csharp
public EnemyState State { get; private set; } = EnemyState.Inactive;
public bool IsActive => gameObject.activeSelf && State != EnemyState.Dead;
```

替换为：

```csharp
public EnemyState State { get; private set; } = EnemyState.Inactive;
public float StateEnteredAt { get; private set; }
public bool IsActive => gameObject.activeSelf && State != EnemyState.Dead;
```

`StateEnteredAt` 保存进入当前状态时的 `Time.time`。后续实现“搜索持续几秒”和“调查超时”时，不需要每个状态再单独增加一套计时变量。

### 4.2 增加 SetState 方法

在 `Update()` 方法之前加入：

```csharp
private void SetState(EnemyState next)
{
    if (State == next)
    {
        return;
    }

    State = next;
    StateEnteredAt = Time.time;
}
```

作用：

- 所有状态切换统一从一个入口发生；
- 重复设置相同状态时不会重置进入时间；
- 后续可以在这里集中增加动画切换和调试日志；
- 避免不同代码位置忘记同步状态相关数据。

不要把该方法写成 `public`。目前只有 `EnemyAgent` 自己有权改变状态，其他组件只能提供感知信息。

## 五、替换 EnemyAgent 中的直接赋值

在 `EnemyAgent.cs` 中按顺序修改。

### 5.1 出生状态

找到：

```csharp
State = EnemyState.Wander;
```

替换为：

```csharp
SetState(EnemyState.Patrol);
```

### 5.2 攻击状态

找到：

```csharp
State = EnemyState.Attack;
```

替换为：

```csharp
SetState(EnemyState.Attack);
```

### 5.3 追击与巡逻状态

找到：

```csharp
State = distance < 32f ? EnemyState.Chase : EnemyState.Wander;
```

替换为：

```csharp
SetState(distance < 32f ? EnemyState.Chase : EnemyState.Patrol);
```

这里暂时保留原来的 32 米距离判断，所以本步骤不会改变实际追击逻辑。

### 5.4 死亡状态

找到：

```csharp
State = EnemyState.Dead;
```

替换为：

```csharp
SetState(EnemyState.Dead);
```

### 5.5 回收状态

找到：

```csharp
State = EnemyState.Inactive;
```

替换为：

```csharp
SetState(EnemyState.Inactive);
```

### 5.6 检查是否还有 Wander

在编辑器中全局搜索：

```text
EnemyState.Wander
```

这时测试文件里还会剩下一处，下一节处理。`EnemyAgent.cs` 中不应再有 `EnemyState.Wander`。

## 六、更新对象池测试

打开：

```text
Assets/Tests/PlayMode/CampaignMenuTests.cs
```

搜索：

```csharp
Assert.That(reused.State, Is.EqualTo(EnemyState.Wander));
```

替换为：

```csharp
Assert.That(reused.State, Is.EqualTo(EnemyState.Patrol));
Assert.That(reused.StateEnteredAt, Is.GreaterThanOrEqualTo(0f));
```

第一行验证对象池复用后的敌人回到巡逻状态；第二行验证统一状态入口记录了进入时间。

再次全局搜索：

```text
EnemyState.Wander
```

正确结果应为零处。

## 七、检查最终 EnemyAgent 关键结构

不需要整份覆盖文件，只检查关键流程是否符合：

```csharp
public EnemyState State { get; private set; } = EnemyState.Inactive;
public float StateEnteredAt { get; private set; }

private void SetState(EnemyState next)
{
    if (State == next)
    {
        return;
    }

    State = next;
    StateEnteredAt = Time.time;
}
```

并确认：

- 出生调用 `SetState(EnemyState.Patrol)`；
- 近距离调用 `SetState(EnemyState.Attack)`；
- 32 米内调用 `SetState(EnemyState.Chase)`；
- 32 米外调用 `SetState(EnemyState.Patrol)`；
- 死亡调用 `SetState(EnemyState.Dead)`；
- 回收调用 `SetState(EnemyState.Inactive)`。

## 八、回到 Unity 编译

1. 打开 Unity Hub。
2. 打开 `MazeEscape` 工程。
3. 等待右下角导入和编译进度结束。
4. 打开 `Window > General > Console`。
5. 点击 Console 左上方的清理按钮。

成功标准：

- 没有红色编译错误；
- 不出现 `EnemyState` 没有 `Wander` 定义的错误；
- 不出现括号或分号错误。

如果看到类似：

```text
'EnemyState' does not contain a definition for 'Wander'
```

说明还有文件保留旧状态名。双击错误定位，将对应的 `Wander` 改成 `Patrol`。

## 九、运行游戏人工验收

1. 打开 `Assets/Scenes/Main.unity`。
2. 点击 Unity 顶部中央的三角形 Play 按钮。
3. 点击开始，选择第一关。
4. 选择冲锋枪并进入迷宫。
5. 接近一只僵尸。
6. 确认僵尸仍会靠近玩家并攻击。
7. 击败僵尸，等待它从画面消失。
8. 继续游玩，确认对象池重新使用敌人时没有报错。
9. 再次点击 Play 按钮退出运行模式。

本步骤不要求僵尸表现得更聪明。玩家体验与改动前基本一致才是正确结果。

## 十、运行自动化测试

在 Unity 中：

1. 点击 `Window > General > Test Runner`。
2. 打开 `PlayMode` 标签。
3. 点击 `Run All`。
4. 等待全部测试结束。

正确结果：

```text
35 passed
0 failed
```

如果 `PooledZombie_ResetsDeathAndSwitchesToEliteModel` 失败，先检查测试预期是否已经从 `Wander` 改为 `Patrol`。

## 十一、提交前检查

关闭 Unity 的运行模式，在 PowerShell 执行：

```powershell
Set-Location -LiteralPath "D:\software\documents\unity_games\projects\learning\MazeEscape"
git status
git diff --check
git diff -- Assets/Scripts/Core/GameTypes.cs Assets/Scripts/AI/EnemyAgent.cs Assets/Tests/PlayMode/CampaignMenuTests.cs
```

预期只有以下三个文件被修改：

```text
Assets/Scripts/Core/GameTypes.cs
Assets/Scripts/AI/EnemyAgent.cs
Assets/Tests/PlayMode/CampaignMenuTests.cs
```

如果还有 `.slnx`、`Library`、`Temp` 或其他无关文件，不要一起 `git add Assets`，先停下来检查。

## 十二、提交并推送

确认人工验收和 `35/35` 测试通过后执行：

```powershell
git add Assets/Scripts/Core/GameTypes.cs Assets/Scripts/AI/EnemyAgent.cs Assets/Tests/PlayMode/CampaignMenuTests.cs
git commit -m "refactor: expand enemy behavior states"
git push
git status
```

最终应看到：

```text
On branch feature/intelligent-enemy-ai
Your branch is up to date with 'origin/feature/intelligent-enemy-ai'.
nothing to commit, working tree clean
```

本步骤完成后不要创建 Pull Request。视觉、声音、记忆、群体警报和 Utility AI 还会继续提交到同一个功能分支。

## 十三、完成后必须能解释

不要只记代码，至少能用自己的话回答：

1. 为什么把 `Wander` 改为 `Patrol`？
2. `Investigate`、`Chase` 和 `Search` 的区别是什么？
3. 为什么所有状态切换都通过 `SetState`？
4. 为什么重复进入相同状态时不能重置 `StateEnteredAt`？
5. 为什么本步骤先定义状态，但不立刻实现全部功能？
6. 对象池复用敌人时，为什么必须重新进入 `Patrol`？

能够回答这些问题，才算真正完成第一步。
