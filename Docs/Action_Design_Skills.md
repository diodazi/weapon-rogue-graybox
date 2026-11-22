# 动作指导技能配置

配置日期：2026-10-18（Asia/Shanghai）。本轮任务是获取和配置动作指导技能；不是正式招式设计或动画制作验收。

## 技能及用途

| 技能 | 来源 | 用途 | 项目副本 |
|---|---|---|---|
| `weapon-motion-2d` | 本轮创建的专用技能 | 中西兵器形制研究、3D 动作转 2D、武器本体运动、关键姿势及规则交接 | [入口](../Tools/ActionSkills/weapon-motion-2d/SKILL.md) |
| `motion-reference-analysis` | 本轮创建的专用技能 | 视频/图片/动捕参考拆解，记录轨迹、节奏、接触与衔接，区分观察和改编 | [入口](../Tools/ActionSkills/motion-reference-analysis/SKILL.md) |
| `animator-traditional` | Dylan Tarre 的 MIT 开源技能 | 传统动画原则、关键姿势、弧线、随动、时间节奏与剪影可读性 | [入口](../Tools/ActionSkills/animator-traditional/SKILL.md) |

这三项是工作流程与参考方法，不代表新增了模型训练，也不代表对所有武术门派、兵器或动画软件完成专业认证。

开源技能来源：[dylantarre/animation-principles](https://github.com/dylantarre/animation-principles/tree/83597134ba8ff59838270f94d7ac7282ffa3b54d/skills/03-by-role-persona/animator-traditional)。固定提交 `83597134ba8ff59838270f94d7ac7282ffa3b54d`。保留原始 `SKILL.md` 和 MIT 许可证，增加中文界面描述；未引入仓库其他技能或可执行程序。

项目副本用于查看、交接和复现。实际自动发现入口安装在 `C:/Users/Administrator/.codex/skills/` 下的同名目录；修改项目副本不自动更新安装副本。后续维护需核对两处内容，避免版本漂移。

安装验证：三项技能均通过官方结构校验；本地相对引用、中文界面描述和调用提示检查通过；安装后的 10 个文件与项目副本逐文件 SHA-256 一致。全部为 Markdown、YAML 或许可证文本，没有安装额外可执行程序。新技能从下一轮对话起可供选择；这不等同于动作表现或软件管线已通过实机验收。

## 如何配合现有项目

依据当前 [GDD](GDD_MVP.md) 和 [项目记录](Project_Memory.md)：

- Unity、2D 地面横板；玩家是刀、剑、矛本体。人体动作参考应转译为武器自身的根位移、朝向、虚拟支点、惯性表现和回收，不添加持武器的人形角色。
- 保留儿童涂鸦、纸张拼贴方向；像素技能不作为动作指导的默认入口。
- 动作沿用受控物理和现有战斗规则；动画目标不能绕过速度/偏转限制，也不能擅自改变刃柄分区、碰刃弹反、取消窗口或冲刺保护。
- 先对齐形制、动作意图、关键姿势和路径，再交给现有美术/客户端流程。素材导入需要时才调用已有 Unity Sprite 编辑等技能。
- 传统动画原则要按材质和玩法选择应用。重量不等于全程慢速；刚性兵器不要求挤压拉伸；自然的刺击可以接近直线；跟随特效不能延长攻击判定。
- 当前正式手绘参考尚待提供。技能已具备分析流程，但尚无本轮用户动作素材可供逐帧研究，也没有本轮正式动画成片。

旧岗位表中的“实际开发尚未开始”等阶段描述早于当前 GDD；未来任务以最新用户指令和现行项目文档为准。本文件只补充技能配置，不复制一份新的战斗规则。

## 后续使用方式

可以直接说：

- “按动作指导流程，分析这段长枪视频，再改成武器本体的横板动作。”
- “使用 `$weapon-motion-2d`，设计刀的起手、斩击、制动和连招连接，先给关键姿势。”
- “使用 `$motion-reference-analysis`，比较这两份参考的发力表现和节奏，标明实际看得到的证据。”
- “使用 `$animator-traditional`，检查这组手绘关键帧为什么显得机械或没有重量。”

参考分析的交付包括：来源与可见范围、关键时刻、端点路径、节奏、终态衔接、2D 改编理由和待验证项。需要归档时保存为项目动作资料，不将“看过一次”描述为模型永久学习。

## 研究来源

- [国际武联：武术套路](https://iwuf.org/sport-wushu/competitive-wushu/taolu/)：兵器类别与官方资料入口。
- [Getty：Fiore dei Liberi 的剑术手稿图页](https://www.getty.edu/art/collection/object/108HNW)：欧洲历史剑术的原始文献入口。
- [The Met：兵器与盔甲常见误解](https://www.metmuseum.org/toah/hd/aams/hd_aams.htm)：核查兵器形制、重量等背景假设。
- [Animation Mentor：动画十二原则](https://www.animationmentor.com/blog/tutorial-12-principles-of-animation/)：动画术语与表现方法。

以上已核验网页内容或目录；不是已经逐帧学习完成的视频库。实际使用具体动作时仍需查看对应图页、演示与上下文。
