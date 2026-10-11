# Maze Escape / 迷宫逃生文档入口

**当前正式版 v1.3.0：先看 [13_V1_3_0_SETTINGS.md](13_V1_3_0_SETTINGS.md)。** 默认中文，支持英文切换、静音、总音量、音乐与音效音量，并保留十关迷宫、全图避难灯、装弹反馈和此前的战斗修复。

- [v1.3.0 Release 下载](https://github.com/ff1333/FogboundMaze/releases/tag/v1.3.0)
- [v1.3.0 自动测试、平台构建和真机验收边界](test-results/v1.3.0/README.md)
- [v1.2.2 避难灯和装弹反馈](12_V1_2_2_LIGHTS_AND_RELOAD.md)（历史）
- [v1.2.1 右手武器与命中反馈](11_V1_2_1_WEAPONS.md)（历史）
- [v1.2.0 图鉴和表现优化](10_V1_2_0_POLISH.md)（历史）

历史版本 **v1.1.0** 见 [09_V1_1_0_MENU_AND_CHARACTERS.md](09_V1_1_0_MENU_AND_CHARACTERS.md)：开始界面、十关选择、本地通关记录、新角色，以及当时的复测和发布步骤。
当时的开发日志见 [devlogs/07-campaign-menu-and-character-art.md](devlogs/07-campaign-menu-and-character-art.md)。
`08_V1_0_3_HUD_AND_BRANCHES.md` 是上一版血条、死亡显示和迷宫分岔的历史记录。
上一版入口/防掉落的历史记录见 `07_V1_0_2_ENTRY_FIX.md`，本次继续保留并通过实体通路回归。

建议按下面顺序阅读：

1. `00_GAME_DESIGN.md`：玩法目标、十关范围和难度解锁。
2. `01_ARCHITECTURE.md`：代码模块、运行顺序和关键取舍。
3. `02_CAMPAIGN_AND_BALANCE.md`：每关迷宫与压力参数。
4. `03_TESTING.md`：自动测试、实际 Player 烟测和人工测试边界。
5. `13_V1_3_0_SETTINGS.md` 和 `test-results/v1.3.0/README.md`：当前版本实现、验证和下载边界；`04_BUILD_AND_RELEASE.md` 仅保留首次发布的历史流程。
6. `05_INTERVIEW_TALK.md`：面试时如何用 3-5 分钟讲清项目。
7. `devlogs/`：从算法基线到发布版本的开发留痕。
8. `devlogs/04-first-playtest-bugfix.md`：首次试玩反馈、原因、修复和复测证据。

`04_BUILD_AND_RELEASE.md` 保留 `v1.0.0` 的首次发布流程；首次试玩补丁的交付状态和
步骤见 `06_V1_0_1_PATCH_RELEASE.md`。学习时先运行项目，再按架构文档逐个定位脚本。
