# Database Scripts

`AgentMailbox` 当前以 `FluentMigrator` 作为数据库 schema 的唯一权威来源。

- 运行时迁移定义位于 `AgentMailbox.Repositories/Migrations/`
- 应用启动时会自动执行数据库升级

此目录不再参与运行时建库，仅保留给历史参考或导出脚本使用。
