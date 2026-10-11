# Maze Escape v1.3.1 验证记录

验证日期：2026-10-11

## 自动测试

- EditMode：15/15 通过，0 失败。
- PlayMode：35/35 通过，0 失败。
- 双语标题测试确认中文为“迷宫逃生”，英文为 `MAZE ESCAPE`。

## 构建结果

- Windows：成功，0 errors，0 warnings，输出 `MazeEscape.exe`。
- WebGL：成功，0 errors，0 warnings。
- Android：成功，0 errors，24 条构建链警告，输出 `MazeEscape-v1.3.1.apk`。

## 运行时验证

- Windows Release Player 退出码为 0。
- 运行时版本检查输出 `FOGBOUND_VERSION_PASS 1.3.1`。
- 中文主菜单截图显示“迷宫逃生”和 `v1.3.1`。
- 第 4 关自动路线完成 6 个格子，输出 `FOGBOUND_WALK_PASS`。

## 证据文件

- `EditMode.xml`
- `PlayMode.xml`
- `native/title-zh.png`

Android 警告没有伪写为零。它们没有导致构建失败，但新版 APK 仍应在真机上做一次安装、移动、攻击、暂停和返回测试。
