# Maze Escape v1.4.0 候选版验证记录

验证日期：2026-10-28

状态：功能分支发布候选。以下记录证明自动规则、Windows 构建和基础画面通过，不代表真人手感、多平台兼容或正式发布完成。

## 自动测试

- EditMode：`21/21` 通过，0 失败。
- PlayMode：`42/42` 通过，0 失败。
- 覆盖迷宫连通与复杂度、A*、Utility AI 评分、视觉和遮挡、声音调查、有限警报、感知记忆重置、默认长刀、狙击枪单发与拉栓、手动换弹、配镜减速、手持/背负互斥、环境配置和原有主循环回归。

## Windows Development 构建

- Unity：`6000.3.18f1`。
- 结果：成功。
- errors：`0`。
- warnings：`0`。
- 大小：`170,676,088` bytes。

## 运行时冒烟

- 独立 Player 退出码：`0`。
- 版本检查：`FOGBOUND_VERSION_PASS 1.4.0`。
- 运行时检查：`FOGBOUND_RUNTIME_SMOKE_PASS`。
- 默认长刀、狙击枪第三人称、配镜和手动换弹截图成功；配镜显示圆形遮罩、十字线和剩余专注时间，换弹完成后为 `5 / 19`。

## 证据文件

- `EditMode.xml`
- `PlayMode.xml`
- `native/default-blade.png`
- `native/sniper-third-person.png`
- `native/sniper-scope.png`
- `native/sniper-reload.png`

## 尚未完成

- 作者真人完整试玩和难度调平。
- Android v1.4.0 构建与真机验收。
- WebGL 构建与浏览器交互验证。
- 正式 Windows 非 Development 构建。
- PR 合并、`v1.4.0` 标签和 GitHub Release。

发布前必须把人工试玩结果追加到本目录。不能把自动截图或自动移动写成作者真人验收通过。
