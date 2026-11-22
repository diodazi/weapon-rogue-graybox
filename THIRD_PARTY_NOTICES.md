# 第三方内容与许可证说明

本文件记录仓库内可确认的第三方内容来源。项目整体尚未指定开源许可证；公开源码也不自动授予额外许可。后续引入正式美术、音频、字体或其他工具时，补齐来源、版本和授权范围。

| 内容 | 项目内位置 | 来源及授权记录 |
|---|---|---|
| animator-traditional 技能 | `Tools/ActionSkills/animator-traditional/` | Dylan Tarre，MIT；保留目录内 `LICENSE`，固定来源见 `Docs/Action_Design_Skills.md` |
| 自建动作／参考分析技能 | `Tools/ActionSkills/weapon-motion-2d/`、`motion-reference-analysis/` | 本项目前期制作的流程副本；不依赖这些技能运行游戏 |
| TextMeshPro 基础资源与 Liberation Sans | `WeaponGraybox/Assets/TextMesh Pro/` | 随 Unity TextMeshPro 基础资源导入；保留 `Fonts/LiberationSans - OFL.txt`；字体按 SIL Open Font License 使用 |
| Unity 2D 模板、渲染与编辑资源 | `WeaponGraybox/Assets/` 的模板／设置资源 | 来自 Unity Universal 2D 模板；原资源保留，不将其宣称为本项目原创手绘 |
| Unity UPM 依赖 | `WeaponGraybox/Packages/manifest.json`、`packages-lock.json` | 依赖从原包源恢复，各包授权按其包内许可证与 Unity 条款；未将 `Library/PackageCache` 复制入仓库 |
| 系统中文字体 | 运行时按本机字体名称动态加载 | 优先 Microsoft YaHei／SimHei，未复制系统字体文件；其他系统的字体可用性须单独验收 |

动作技能来源和固定提交详见 [动作指导技能配置](Docs/Action_Design_Skills.md)。Liberation Sans 的字体许可证位于 [LiberationSans - OFL.txt](WeaponGraybox/Assets/TextMesh%20Pro/Fonts/LiberationSans%20-%20OFL.txt)。整体发行许可、正式素材商业授权与发布平台要求，在正式发布前单独完善。
