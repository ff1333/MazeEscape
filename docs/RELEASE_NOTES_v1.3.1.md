# Maze Escape v1.3.1

## 本次更新

- 游戏对外名称统一为《迷宫逃生》/ `Maze Escape`。
- Windows 可执行文件与三平台发布附件统一使用 `MazeEscape` 文件名。
- 主菜单、双语文本、README、口播稿、简历与发布指南同步更新。
- 保留 v1.3.0 的十关迷宫、双视角、冲锋枪与长刀、A* 敌人追踪、探索小地图、浓雾、昼夜、瘴气、避难灯和声音设置。

## 兼容说明

- GitHub 仓库由 `FogboundMaze` 改名为 `MazeEscape` 后，GitHub 会自动重定向旧链接。
- C# 内部命名空间和 Android application identifier 保持不变，避免无收益的序列化与存档兼容风险。
- 本次改名不清除本地关卡进度或声音设置。

## 发布附件

- `MazeEscape-Windows-v1.3.1.zip`
- `MazeEscape-WebGL-v1.3.1.zip`
- `MazeEscape-Android-v1.3.1.apk`
- `SHA256SUMS.txt`

## 验证结果

- EditMode：`15/15 PASS`
- PlayMode：`35/35 PASS`
- Windows：`Succeeded, 0 errors, 0 warnings`
- WebGL：`Succeeded, 0 errors, 0 warnings`
- Android：`Succeeded, 0 errors, 24 build-chain warnings`
- Windows 运行时：版本检查、中文标题截图与启动烟雾测试通过
