# 首轮灰盒测试报告

报告日期：2026-10-18（Asia/Shanghai）。对应 GDD v0.3。本文记录工程实现、临时实验选择与验证证据，不修改已确认设计基线。

> 2026-11-22 归档提示：这是首轮历史报告。最新试玩已确认背后攻击、跳跃躲避、残影穿身和弹反反馈四项待修，见 [Graybox_Issues.md](Graybox_Issues.md)。历史自动测试通过不代表这些体验条件已经满足。源码上传前的规则复跑另见 `WeaponGraybox/Evidence/rule-upload-check.json`。

**当前报告状态：本轮可试玩灰盒已交付。** 独立规则 **28 / 28**、Unity 场景集成 **25 / 25**、Windows 独立程序 **25 / 25** 通过；已目视检查编辑器的选角／大厅／战斗截图，以及最终独立程序的选角／战斗截图，中文与几何武器显示正常。自动检查不等于手感、难度或完整多人功能通过验收。

## 1. 实施范围与环境

| 字段 | 当前事实／边界 |
|---|---|
| 工程 | `D:/gun and gun/WeaponGraybox` |
| Unity | `6000.6.5f1`，来自工程 `ProjectVersion.txt` |
| 渲染与输入 | URP 2D 工程；Input System；uGUI + TextMeshPro |
| 当前输入 | 单人键盘战斗；鼠标可操作界面按钮 |
| 当前流程 | 刀／剑／矛选择→大厅练习→最右准备区自行确认→一个测试 Boss→结果→返回大厅 |
| 当前形体 | 几何占位；刃、柄受击区域分开；受控物理转动与位移 |
| 当前模式 | 高难度／派对两个玩家血量配置；数值未经过平衡验证 |
| 构建目标 | Windows x64 开发测试包；不据此将正式 PC 产品支持范围缩为仅 Windows |
| 未接入 | 手柄、本地多人、实际友伤多人场景、救援、奖励卡、技能树、后续 Boss、正式美术与音频 |

实现存在不等于验收通过。纯规则测试不覆盖 Unity 物理接触、真实按键输入、动画可读性、界面布局、运行帧率或玩家体验。

## 2. 证据与验收状态

| 验证层 | 状态 | 证据／待填写内容 | 可以支持的结论 |
|---|---|---|---|
| 独立战斗规则 | PASS：28 / 28 | `WeaponGraybox/Evidence/rule-results.json` | 下节列出的状态与结算断言通过；仅针对当次运行的测试范围 |
| Unity 脚本编译 | PASS | `WeaponGraybox/Logs/GrayboxEditor.log`；实际进入运行并完成集成探针 | 当前灰盒脚本可编译运行 |
| Unity 真实运行与碰撞 | PASS：25 / 25 | `WeaponGraybox/Evidence/playmode-results.json` | 实际物理移动、跳跃、碰刃、受击、准备区转场；输入通过与键盘相同的动作队列注入，不冒充实体键盘人工试玩 |
| 中文界面与截图 | PASS：本机 1600×900 | 已目视检查 `selection.png`、`lobby.png`、`combat.png`；TMP 基础资源及系统字体加载已修复 | 中文可读、按钮及状态框无重叠；不代表其它字体环境与分辨率已覆盖 |
| Windows 构建 | PASS：Succeeded | `WeaponGraybox/Evidence/build-result.json`；0 错误、3 警告 | Windows x64 开发包已生成，约 189 MB；文件在 `Builds/Windows/` |
| 独立试玩包运行 | PASS：25 / 25 | `WeaponGraybox/Evidence/standalone-results.json`、`standalone-player.log` | 已实际启动 exe，在独立程序中复验同组输入与物理场景 |
| 独立试玩包画面 | PASS：本机 1600×900 | 已目视检查 `standalone-selection.png`、`standalone-combat.png` | 打包后中文、材质、刃柄轮廓及界面显示正常；后台截图改用相机离屏渲染 |
| 手感与数值平衡 | 未验收 | 用户试玩反馈与后续对照实验 | 自动测试通过不能证明弹反可读性或难度已经合适 |

构建的 3 条警告为：两处测试代码使用的 `DEVELOPMENT_BUILD` 条件在此 Unity 版本中已被标记为将弃用；一处提示未开启 Pipeline 在独立程序中的远程控制。灰盒不依赖独立程序远程控制，当前测试包仍已成功构建运行。编辑器日志中的历史字体异常已修复，不应与最终独立运行日志混用。

本轮修复并复验了三个问题：同组失败命中因第一击触发破势而产生顺序相关增伤；同一物理步内先按后松 S 仍留下蓄力；霸体攻击前摇缺少可区分提示。弹反探针最初使用墙钟等待，没有可靠复现目标出招状态，后改为固定模拟时刻输入；五组轨迹保存在 `Evidence/parry-trajectories.csv`，覆盖过早输入、普通和精确弹反。此记录用于定位，不当作玩家成功率统计。

## 3. 独立规则测试清单

测试入口：`Tools/RuleChecks/Program.cs`；执行工程：`Tools/RuleChecks/RuleChecks.csproj`。规则实现位于 `WeaponGraybox/Assets/Graybox/Runtime/CombatCore.cs`。

以下结果来自报告整理时读取的 `rule-results.json`。规则代码或测试代码变更后应重新执行，并更新本表及汇总数量。

| ID | 检查内容 | 结果 |
|---|---|---|
| RULE-01 | 弹反保护只阻止血量伤害，不阻止架势增长及控制效果 | PASS |
| RULE-02 | 普通冲刺中段没有无敌保护 | PASS |
| RULE-03 | 蓄力冲刺仅在中段同时阻止血量、命中架势、击退、打断 | PASS |
| RULE-04 | 普通与蓄力冲刺共用两次机会 | PASS |
| RULE-05 | 重复冲刺指令不能重复扣除或生成冲刺 | PASS |
| RULE-06 | 重复模拟步编号不能加快回充 | PASS |
| RULE-07 | 临时串行回充每个到期点仅恢复一次，且不超过上限 | PASS |
| RULE-08 | 提前退出冲刺仅撤销冲刺保护，不误清其它保护 | PASS |
| RULE-09 | 松开蓄力与执行未强化操作会清空蓄力 | PASS；其中松 S 的具体策略属于临时实验选择 |
| RULE-10 | 未蓄满不产生蓄力冲刺效果 | PASS |
| RULE-11 | 破势期间反复受击不延长固定失控时间 | PASS |
| RULE-12 | 破势结束恢复操作、架势清零并继续锁定，不附加血量保护 | PASS |
| RULE-13 | 霸体仍受血量伤害，但锁定架势并免疫命中控制 | PASS |
| RULE-14 | 显式脚本在霸体期间将架势置满，等霸体结束再破势 | PASS |
| RULE-15 | 刃／柄只改变血量伤害，不改变同一普通命中的架势量 | PASS |
| RULE-16 | 一攻二守且都成功时，攻击方只增加一次弹反架势 | PASS |
| RULE-17 | 本次新增弹反保护不会吞掉同次交锋的失败防守伤害 | PASS |
| RULE-18 | 二攻一守时，另一组防守失败仍会受击 | PASS |
| RULE-19 | 交锋前已存在的血量保护仍对失败命中生效 | PASS |
| RULE-20 | 同次交锋重复提交同一攻击关系只结算一次 | PASS |
| RULE-21 | 霸体攻击不能被弹反 | PASS |
| RULE-22 | 大厅无限血量不等于架势免疫 | PASS |
| RULE-23 | 跨越多个时间边界的单次推进保留破势与恢复时间顺序 | PASS |
| RULE-24 | 非法数值状态会被拒绝 | PASS |
| RULE-25 | 两路失败命中置换顺序，不让第一击破势改变同组第二击倍率 | PASS；示例两顺序均为 71.5 HP |
| RULE-26 | 普通先后受击仍保留第一击破势、下一击增伤语义 | PASS |
| RULE-27 | 已有破势、霸体、残影和 HP 保护下的交锋置换结果一致 | PASS |
| RULE-28 | 同次交锋中的致死命中顺序不改变总伤害 | PASS |

多人相关条目验证的是独立结算模型；当前实际场景只有玩家和一个测试对象，尚未接入三名玩家的接触收集与输入。因此这些条目不构成多人完整功能验收。

## 4. Unity 运行验收表

自动探针直接投递输入队列，使用真实 Rigidbody2D 和刃／柄碰撞。对位置的设置仅建立试验初始条件，不作为移动通过的证据。胜负分支通过脚本扣血建立终止条件，未模拟一整局自然对战。自动检查没有覆盖的细项继续标待验。

| ID | 操作与观察 | 通过条件 | 当前结果 |
|---|---|---|---|
| PLAY-01 | 启动、选角、切换模式 | 刀／剑／矛可选，中文可读，选定后先进入大厅 | PASS：三武器、派对 HP 与中文字符检查；截图已检查 |
| PLAY-02 | 移动、跳跃动作队列 | 实际左右移动、地面跳跃；W 不触发跳跃 | 物理移动和跳跃 PASS；实体按键映射仍需人工试玩 |
| PLAY-03 | 大厅攻击练习对象 | 实际接触产生反馈，双方保持无限血量；架势规则仍存在 | 无限血量与真实碰刃 PASS；详见轨迹记录 |
| PLAY-04 | 在准备区外确认，再到最右区域确认 | 区外不进入战斗；区内确认后开始 | PASS：区域判断与确认转场；实体按键／鼠标点击待人工试玩 |
| PLAY-05 | 从大厅进入 Boss 战 | 玩家与 Boss 恢复有限血量 | PASS |
| PLAY-06 | 普通冲刺与完整蓄力冲刺 | 共用机会、消耗一次；只有蓄力冲刺中段产生残影保护 | PASS：保护、消耗、位移、结束撤销、回充 |
| PLAY-07 | 快速按下松开 S、未满蓄力后移动 | 蓄力不会残留 | PASS；其它取消组合仍需扩展 |
| PLAY-08 | 同次攻击接触 | 同一攻击对同一目标不重复扣血 | PASS：一次轻攻击实测刃命中 7.8 HP，后续不重复 |
| PLAY-09 | 有效窗口内实际碰刃，与同输入但不接触对照 | 仅实际接触且满足窗口时出现弹反／精确弹反 | PASS：普通、精确、无接触三组；基于固定模拟时刻投递输入 |
| PLAY-10 | 触发破势、霸体与状态结束 | 画面提示与实际可操作状态、锁定及受伤一致 | 待验证 |
| PLAY-11 | Boss 暂停／继续、轮廓切换、重新选角 | 按钮实际生效；暂停决策不突然删除已开始攻击 | 暂停及重新选角逻辑已用于探针；实际鼠标交互待人工试玩 |
| PLAY-12 | 玩家倒下／Boss 倒下后返回大厅 | 可进入正确结果并重新练习 | PASS：脚本建立死亡条件，真实 FixedUpdate 判胜负 |
| PLAY-13 | 独立 Windows 包运行 | 能启动、进入大厅和 Boss 战，完成同组场景检查 | PASS：25 / 25；最终包选角与战斗画面已目视复验 |

## 5. 灰盒参数快照

来源：`GrayboxSettings.cs`、`CombatCore.cs` 及已导出的实际配置 `WeaponGraybox/Evidence/settings-snapshot.json`。运行时以 `Assets/Graybox/Settings/PrototypeSettings.asset` 为准；修改资产后同步本表，避免把源码默认值误当作实际测试值。

状态：`CONFIRMED` 为已确认常量；`PROVISIONAL` 为用户暂定值；其余都是 `PROTOTYPE_ONLY`，只用于本次试验，不回写为正式设计。

| 参数 ID | 当前默认值 | 单位 | 状态／用途 |
|---|---:|---|---|
| `DashCapacity` | 2 | 次 | CONFIRMED；普通／蓄力冲刺共享上限 |
| `DashRechargeSeconds` | 1.5 | 秒／次 | PROVISIONAL |
| `HardPlayerHP` | 100 | HP | PROTOTYPE_ONLY；高难度玩家 |
| `PartyPlayerHP` | 240 | HP | PROTOTYPE_ONLY；派对玩家 |
| `BossHP` | 420 | HP | PROTOTYPE_ONLY；两模式共用 |
| `BladeDamageMultiplier` | 0.65 | 倍 | PROTOTYPE_ONLY；命中刃 |
| `HiltDamageMultiplier` | 1.4 | 倍 | PROTOTYPE_ONLY；命中柄 |
| `LightDamage` / `HeavyDamage` | 12 / 24 | HP 基值 | PROTOTYPE_ONLY；再应用区域与状态倍率 |
| `OrdinaryHitPosture` | 3 | 架势 | PROTOTYPE_ONLY |
| `MaxPosture` | 100 | 架势 | PROTOTYPE_ONLY |
| `ParryAttackerPressure` / `ParryDefenderPressure` | 24 / 8 | 架势 | PROTOTYPE_ONLY |
| `PreciseParryExtraPressure` | 16 | 架势 | PROTOTYPE_ONLY；攻击方额外增长 |
| `ParryProtectionDuration` / `ParryRecovery` | 0.06 / 0.23 | 秒 | PROTOTYPE_ONLY；血量保护短于恢复 |
| `PostureDecayDelay` / `PostureDecayPerSecond` | 1.2 / 20 | 秒／架势每秒 | PROTOTYPE_ONLY |
| `BrokenDamageMultiplier` | 1.5 | 倍 | PROTOTYPE_ONLY |
| `BreakDuration` / `PostBreakLockDuration` | 1.1 / 0.8 | 秒 | PROTOTYPE_ONLY |
| `ChargeDuration` | 0.65 | 秒 | PROTOTYPE_ONLY |
| `DashStartupDuration` / `DashMiddleDuration` / `DashRecoveryDuration` | 0.05 / 0.18 / 0.07 | 秒 | PROTOTYPE_ONLY；保护仅中段 |
| `MoveSpeed` / `DashSpeed` | 5.5 / 15 | 世界单位／秒 | PROTOTYPE_ONLY；受控物理目标速度 |
| `MoveAcceleration` / `JumpImpulse` | 48 / 8.5 | 加速度／冲量 | PROTOTYPE_ONLY |
| `AngularGain` / `AngularAcceleration` | 30 / 18000 | 控制参数 | PROTOTYPE_ONLY；转动控制与上限 |
| `LightStartup` / `LightActive` / `LightRecovery` | 0.18 / 0.26 / 0.28 | 秒 | PROTOTYPE_ONLY |
| `HeavyStartup` / `HeavyActive` / `HeavyRecovery` | 0.36 / 0.32 / 0.40 | 秒 | PROTOTYPE_ONLY |
| `CancelWindowStart` / `CancelWindowEnd` | 0.02 / 0.10 | 动作开始后秒数 | PROTOTYPE_ONLY；当前跳跃／冲刺取消窗口 |
| `DeriveBuffer` | 0.16 | 秒 | PROTOTYPE_ONLY；收招末段的派生输入窗口 |
| `HitStun` / `Knockback` | 0.13 / 3.4 | 秒／水平冲量 | PROTOTYPE_ONLY |
| `ApproachImpulse` | 1.1 | 水平冲量 | PROTOTYPE_ONLY；起手附带接近位移 |
| `BossStartup` / `BossActive` / `BossRecovery` | 0.70 / 0.32 / 0.65 | 秒 | PROTOTYPE_ONLY |
| `BossThinkDelay` / `BossMoveSpeed` | 0.65 / 2.4 | 秒／世界单位每秒 | PROTOTYPE_ONLY |
| `BossDamage` / `BossAttackRange` | 18 / 2.8 | HP 基值／世界单位 | PROTOTYPE_ONLY |
| `BossArmorDuration` | 3.2 | 秒 | PROTOTYPE_ONLY；低于一半血量后触发一次 |
| `NormalParryInputEarliest` / `NormalParryInputLate` | 0.08 / 0.10 | 秒 | PROTOTYPE_ONLY；从来袭起手后 0.08 至其前摇结束后 0.10 接受防守输入 |
| `PreciseParryInputLead` / `PreciseParryInputLate` | 0.24 / 0.04 | 秒 | PROTOTYPE_ONLY；精确输入范围为来袭前摇结束前 0.24 至结束后 0.04，并仍需满足普通窗口和真实接触 |
| `fixedDeltaTime` | 0.01 | 秒 | PROTOTYPE_ONLY；100 Hz 物理步进；不代表已保证所有机器稳定 100 Hz |

几何尺寸也为临时参数，尚未依据参考图制作：

| 形体 | 刃长 | 柄长 | 说明 |
|---|---:|---:|---|
| 刀 | 1.15 | 0.55 | 世界单位；矩形判定 |
| 剑 | 1.50 | 0.55 | 世界单位；矩形判定 |
| 矛 | 0.55 | 1.55 | 世界单位；长柄、小刃 |
| 测试 Boss | 1.80 | 0.70 | 世界单位；临时长刃形体 |

## 6. 尚未定案的分支在原型中的临时选择

以下全部为 `PROTOTYPE_ONLY`。这是为让可执行模型闭合而做的实验选择，不是用户已经确认的正式规则。

| 边界 | 本次实验选择 | 后续要求 |
|---|---|---|
| 两次冲刺回充如何调度 | 串行，每到期一次恢复一段；未满时继续下一段计时 | 与其它调度方式对照后再决定 |
| 松开 S、未满按 L、持续按住 S | 松开即清空；未满可发动普通冲刺；冲刺后需要重新按 S 开始下一次蓄力 | 确认操作反馈和实际体验；不改变“必须蓄满才有强化” |
| 同一攻击同时重叠目标刃和柄 | 柄优先；同一攻击对同一目标一次结算 | 复查是否导致长柄武器过度受罚 |
| 一攻多守中普通与精确结果混合 | 攻击方合并取较高的单次压力，不逐个叠加 | 仅作为独立规则模型试验；正式合并细则仍待定 |
| 两个攻击方同时被同一防守者防住 | 防守方暂按各攻击关系相加 | 正式防守方合并细则仍待定 |
| 三人交锋分组 | 独立规则接口接受同次交锋的关系集合；实际单人场景仍为两单位接触 | 多人接入时须定义时间边界、事件收集及跨帧去重 |
| 两方同刻起手 | 当前单人接触代码以稳定调用顺序打破先后相同 | 正式先攻方识别仍待定 |
| 破势中尝试新增霸体 | 不用新霸体取消已经开始的固定破势 | 状态重叠策略仍待确认 |
| 双方同时倒下 | 本次实验优先判玩家失败 | 正式胜负优先级仍待定 |
| 大厅练习对象 | 增加不主动攻击、无限血量的几何练习对象 | 测试辅助，不当作正式大厅内容需求 |
| 当前动作样例 | 共用基础动作；轻攻击最多四段样例，末段提供短霸体 | 不是三武器正式连招表；待手绘参考与实测调整 |

## 7. 边缘测试／漏洞预判

| 风险 | 已有防范／证据 | 仍需补充 |
|---|---|---|
| 持续重叠或重复碰撞造成无限伤害、架势爆涨 | 攻击实例记录已结算目标；独立交锋按关系去重，攻击方压力另行合并 | Unity 实际持续碰撞与多单位接触必须实测；不能只用模型结论代替 |
| 取消冲刺或重复回调复制次数／保护 | 共享次数；有效发动与扣除一次提交；模拟步去重；退出撤销该来源保护；规则测试覆盖 | 实际键盘快速输入、低帧率和场景切换复验 |
| 同次新增保护误吞其它失败关系 | 失败结果尊重交锋前已有保护，新弹反保护后施加；规则测试覆盖 | 多人场景接触收集及混合状态需再次验收 |
| 看似碰刃却因为物理轨迹与时间窗口错位而失败 | 实际碰刃为必要条件，界面记录交锋反馈 | 调试各武器的输入提前量、前摇、旋转与接触窗口；不能宣称当前手感已合格 |
| 长柄武器的大柄区域导致明显劣势 | 刃柄参数独立且可调 | 用相同样本比较受伤分布、弹反率与通关耗时后再定平衡 |
| 几何支撑体、墙角、跳跃和冲刺叠加导致卡住或穿出 | 出界有安全回收路径 | 在三种形体和不同帧率下验证，记录碰撞与回收触发 |
| 中文字体缺失或界面超出屏幕 | 当前有本机字体回退路径 | 构建包与不同系统字体、窗口尺寸需要实际验证 |
| 将单人原型误当作完整 MVP 或平衡结论 | 文档逐项标记未接入与未验收 | 后续单独执行手柄、多人友伤、救援、奖励卡及技能树验收 |
