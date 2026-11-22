# 武器本体动作游戏 · Unity 灰盒

玩家直接操控刀、剑或矛，在 2D 横板场景中通过受控物理攻击、真实碰刃弹反和蓄力冲刺挑战 Boss。完整产品目标包含 Boss 连战、随机卡牌形成的当局技能树，以及 1–3 人本地同屏互伤。

代码仓库：[diodazi/weapon-rogue-graybox](https://github.com/diodazi/weapon-rogue-graybox)，公开，主分支 `main`。

**当前是键盘单人、一个测试 Boss 的几何灰盒。正式游戏名称待定。** 美术目标为儿童涂鸦、纸张背景与撕纸拼贴；当前几何外形用于验证机制，正式手绘与动作依参考图完善。

## 当前版本与已知问题

已接通刀／剑／矛选择、大厅无限血量练习、最右准备区域确认、Boss 战、胜负返回、刃柄受击分区、弹反／精确弹反、架势／破势／霸体和共享两次冲刺。

**最新试玩反馈仍待修改：背后攻击经常够不到目标、跳跃不足以躲过低位攻击、蓄力冲刺残影不能穿过单位、弹反缺少回弹与碰撞停顿。** 详细要求及验收见 [灰盒问题清单](Docs/Graybox_Issues.md)。这些反馈优先于旧测试中的通过结论；目前没有把它们标成已修复。

2026-10-18 的历史证据为独立规则 28/28、Unity 场景 25/25、Windows 独立程序 25/25。自动检查覆盖有限，不能证明手感或平衡合格。后续复跑记录放在 [开发指南](Docs/Development_Guide.md) 中。

尚未接入手柄、本地多人、救援、奖励卡、技能树、完整连战和正式音画内容。

## 从源码运行

1. 安装 Unity **6000.6.5f1**，通过 Unity Hub 打开仓库内的 **`WeaponGraybox/`** 文件夹。
2. 等待 Unity 按 `Packages/manifest.json` 与锁文件导入依赖。首次导入需要访问 Unity 包源；`Library/` 不在仓库中。
3. 打开 `Assets/Graybox/Scenes/CombatGraybox.unity`，点击 Play。也可以使用菜单 **Graybox > Create or Open Test Scene**。
4. 按 `1 / 2 / 3` 选刀／剑／矛，`Enter` 进入大厅；到最右侧准备区后再次确认，进入测试 Boss 房间。

| 输入 | 操作 |
|---|---|
| A / D | 移动 |
| S | 原地蓄力 |
| J / U | 轻攻击／重攻击 |
| K | 跳跃；W 不跳跃 |
| L | 普通冲刺 |
| S 蓄满并保持，再按 L | 蓄力冲刺，保护只在残影中段 |
| Enter / Esc | 确认／重新选角 |

高难度／派对当前仅采用不同的临时玩家血量。普通与蓄力冲刺共享 2 次机会，每 1.5 秒恢复一次；1.5 秒仍为暂定值。

## 文档入口

| 文档 | 用途 |
|---|---|
| [MVP GDD](Docs/GDD_MVP.md) | 产品目标、固定术语、机制、Mermaid 状态图、结构化数值与漏洞预判 |
| [开发指南](Docs/Development_Guide.md) | 环境、代码结构、数据入口、实现流程、测试、构建与版本控制 |
| [灰盒问题清单](Docs/Graybox_Issues.md) | 最新试玩问题、已确认修改方向、待定项与回归条件 |
| [项目记忆与决策](Docs/Project_Memory.md) | 已确认、被替换、待定需求及阶段记录 |
| [首轮测试报告](Docs/Graybox_Test_Report.md) | 2026-10-18 的历史运行证据与临时参数 |
| [岗位与技能分工](Docs/Team_Staffing.md) | 制作、策划、视觉、音频、客户端、测试、整合职责 |
| [动作指导技能](Docs/Action_Design_Skills.md) | 参考分析、兵器动作、传统动画的项目副本及来源 |
| [详细试玩说明](WeaponGraybox/README.md) | 操作与状态观察 |

## 测试与构建

仅运行引擎无关规则检查需要 **.NET SDK 10**，不需要启动 Unity。在仓库根目录执行：

```powershell
dotnet run --project Tools/RuleChecks/RuleChecks.csproj -- WeaponGraybox/Evidence/rule-results.json
```

Windows 构建需安装 Unity 对应的 Windows Build Support。在编辑器中退出 Play，执行 **Graybox > Build Windows Test**，产物在 `WeaponGraybox/Builds/Windows/`，结果在 `WeaponGraybox/Evidence/build-result.json`。开发包可通过 `--graybox-self-test` 执行场景探针，具体工作目录及覆盖限制见开发指南。

## 仓库内容与权属

仓库保留 `Assets/` 及 `.meta`、`Packages/`、`ProjectSettings/`、源码、规则检查与精选测试证据。构建包、Unity 缓存、机器日志及本机工具不提交；克隆后请自行构建。

本项目尚未指定整体开源许可证。第三方素材与工具保留其各自许可证，见 [第三方说明](THIRD_PARTY_NOTICES.md)。仓库可见性本身不构成额外的版权授权。
