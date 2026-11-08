# 架构约定与本轮调整

持续开发约定写在项目根目录 AGENTS.md。依据是 Coding/11 中的 SOLID、Vending Machine、Class Basics 和 OODP Relationships 教材。

| 原位置与问题 | 变化会影响什么 | 当前职责安排 |
|---|---|---|
| TableRun 同时包含账单推进、组合收益和商品购买细节 | 改租约、奖励或交易校验都要修改同一个类 | GameSession 协调；RentContract、SwingRewards、ShopInventory 各自处理对应规则 |
| GameHud 同时绘制常驻信息、构建所有弹窗并持有完整规则对象 | 修改 HUD、菜单布局或购买服务容易相互影响 | WorkshopHud、WorkshopMenu、WorkshopStyle 分开；菜单使用只读快照和 IShop / ISkillTree |
| CoinPigGame 通过中文标题识别可返回的弹窗 | 改一个标题会改变按键行为 | WorkshopMenu 使用 MenuPage 和显式返回操作 |
| TableCanvas 混合世界运动、像素坐标、造型、颗粒 | 更换分辨率或 3D 表现会触及运动规则 | PigHerd / PigMotion 处理世界运动；WorkbenchView 负责映射，绘制和反馈分别处理 |
| PigDefinition 保存颜色；业务结果保存提示句子 | 改外观或文案需要修改规则 | PigPainter 与 GameText 负责显示 |
| 实际场景检查位于运行脚本目录 | 发布版本混入自动操作逻辑 | ScenarioRunner 移至 Tests/Scenes，仅 Debug 编译 |

教材强调改变应发生在正确的地方，并不要求一个方法一个类或给每个类套接口。当前设计按独立变化原因拆分，采用真实的持有/使用关系，通过商品目录注入和接口消费验证扩展边界。

## 阅读位置

- AP6412 SOLID_Design_Principles.pdf：14–25 页的 SRP；29 页的协调器；36–39 页的扩展；49–50 页的替换契约；60–61 页的能力接口；65–82 页的依赖方向与取舍。
- Vending_Machine_SOLID_Step_by_Step (1).pdf：2、30、38、43–52 页，分别说明变化局部化、目录配置、入口装配及 UI 的接口依赖。
- CSharp_Class_Basics_Revision.pdf：39–43 页的状态封装和 51 页的应用/实体职责。
- CSharp_OODP_Relationships_Revision.pdf：4、12、20、61 页的 is-a / has-a / uses-a。

## 视觉参考

参考 [CloverPit 的官方商店截图](https://store.steampowered.com/app/3314790/CloverPit/) 中以游戏设备为主体、将信息放在边缘和场景物件附近的布局，以及 [Buckshot Roulette](https://store.steampowered.com/app/2835570/Buckshot_Roulette/) 的桌面场景方向。CoinPig 使用自己的绘制、布局和配色，没有复制参考作品的图像资产。

本轮采用全屏木桌、克制的黄铜色、屏幕边缘的少量状态、收工后出现的装备与技能页；删除常驻大侧栏、营销式副标题和网页式圆角面板。F11 可切换窗口，布局随视口变化。
