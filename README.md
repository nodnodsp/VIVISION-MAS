# MAS V1

多角度测色仪上位机系统 1.0 开发基线。

## 当前已落地内容

1. `MAS.Core`
   领域实体与核心枚举，包含仪器、样品、标准样、容差模板、测量任务、测量记录、角度结果、效果结果、报告导出等基础对象。

2. `MAS.Application`
   首批应用层接口与服务，当前已提供 `MeasurementTaskService`，用于创建测量任务草稿。

3. `MAS.Infrastructure`
   数据库目录结构与 SQLite 建表脚本：
   `MAS.Infrastructure/Database/Schema/mas_schema.sql`

4. 文档资产
   已包含需求文档、页面说明、数据库设计文档与 UI 示意图。

## 当前环境状态

1. `MAS.Core` 可编译。
2. `MAS.Application` 可编译。
3. `MAS.Infrastructure` 和 `MAS.WinUI` 在当前机器上受 .NET / Windows SDK 用户目录权限影响，仍需继续处理。
4. SQLite 执行层目前先以建表脚本形式落地，下一步建议接入 `Microsoft.Data.Sqlite` 或本机可用的 SQLite 执行器。

## 建议的下一步开发顺序

1. 先接通 SQLite 执行层和仓储层。
2. 再实现样品、标准样、容差模板的增删改查。
3. 然后补测量任务与判定流程。
4. 最后接入 WPF 主界面和设备通信层。

## 推荐后续迭代

1. 增加 `CREATE TABLE` 自动执行器。
2. 增加种子数据初始化。
3. 增加 Repository 接口与实现。
4. 增加设备通信抽象层。
