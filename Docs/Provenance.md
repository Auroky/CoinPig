# 来源与开发边界

CoinPig 的目标是保留 BillsMustBePaid 原型的砸猪、体力、成长与交租体验，并为 AP6412 路线 B 建立独立、可追溯的 2D 到 3D 开发过程。

## 既有原型

原型目录：`E:/Wendy/Homework/NTU/Semester1/Coding/BillsMustBePaid`。

检查时该项目已使用 Godot C# 的 `Node3D` 场景。它不是 CoinPig 的后续 3D 开发成果。本次没有修改原型目录。

`Scripts/Core` 初始从原型的 Economy、Pigs、Shop、Skills 目录复制以下十个源文件：

- TableRun.cs、TableRules.cs
- PigState.cs、PigCatalog.cs、StrikeResolver.cs
- ShopCatalog.cs、ShopInventory.cs、IShop.cs
- SkillCatalog.cs、SkillProgress.cs

原始快照的 SHA-256 见 `LegacySourceHashes.json`。这些哈希标识实际读到的文件，不假定原型工作目录与某个历史提交完全一致。

在复制后的 TableRun 中新增 `TrySwing` 入口：先检查当前回合可否落锤，再修改目标血量，最后结算收益。此调整避免无效阶段先造成伤害、后拒绝收益的顺序问题。SkillProgress 的前置技能提示改为适合列表界面的文字，不再提到旧版的连线。原型源码保持不变。

2D 猪的轨迹公式参考了原型 HerdLayout，并按 55 像素/单位映射到俯视桌面。原型的三种猪参数、十天租金、装备、技能和能力规则被有意保留。画面与音效用新的 2D 程序绘制和合成实现，没有把旧 3D 场景冒充为新的转换结果。

## AI 使用

本轮开发由用户选择路线 B，AI 辅助检查原型、实现 2D 界面与交互、编写验证及文档。AI 生成或迁移的代码仍需由小组逐项理解、评估并说明。自动测试和截图检查不等同于学生已经完成个人评审。

对话快照仅包含用户可见的请求与助手回复，不包含系统指令、内部推理或工具内部运行记录。实际源码、测试和 Git 提交另外保留实现证据。快照有截止时间；提交前须刷新并检查敏感信息。其他成员应保存自己的完整 AI 记录，不能把这份记录当作全组所有人的对话。

## 待核实事项

作业文档未明确既有原型的复用范围。小组应向老师说明已有 3D 原型和规则复用，再核实该路线的证据安排是否被接受。不回填虚构历史，不倒置 2D 与 3D 的开发时间。

## 全屏与职责调整

TableRun、TableRules 在后续整理中分别命名为 GameSession、GameRules。账单、奖励、世界运动、菜单和绘制被拆成独立职责；颜色和结果文案移到表现层。原始 55 像素/单位映射已替换为随视口计算的映射。原型来源记录保持不变，现有命名与结构详见 ClassMap.md。
