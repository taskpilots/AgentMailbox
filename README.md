# AgentMailbox

`AgentMailbox` 是一个面向 AI Agent 的邮件域服务骨架项目，用于承载 Agent 邮箱管理、邮件接收、邮件查询、线程上下文与出站发送任务等能力。

当前仓库以 `/Users/duke/gitrepos/Mahjong` 的多工程单仓结构为参考，首轮聚焦“纯后端、最小可运行”框架：

- 后端：`ASP.NET 10` + `AgileLabs.WebApp`
- 数据访问：`Dapper` + `SQLite`
- 后台任务：`BackgroundService`
- 测试：`xUnit`

## 目录结构

```text
AgentMailbox/
  AgentMailbox.Core/         共享契约、领域模型、配置类型
  AgentMailbox.Repositories/ SQLite 仓储抽象与实现
  AgentMailbox.WebApis/      Web API 宿主、控制器、应用服务、后台任务
  AgentMailbox.WebApis.Tests/单元测试
  dbscripts/                 历史数据库脚本与说明
  docs/                      项目与技术文档
```

## 当前能力

- 创建和查询 Agent 邮箱
- 查询邮件列表、邮件详情和线程视图
- 发起新邮件和回复邮件
- 生成待发送任务，供后台分发器后续处理
- 预留入站监听和出站发送后台服务骨架
- 暴露 `/health` 健康检查端点

## 本地开发

### 初始化状态

当前仓库已完成最小后端骨架初始化，并已打通以下本地闭环：

- `dotnet build AgentMailbox.slnx`
- `dotnet test AgentMailbox.WebApis.Tests/AgentMailbox.WebApis.Tests.csproj`
- `dotnet run --project AgentMailbox.WebApis/AgentMailbox.WebApis.csproj`

当前版本对 SQLite 采用“应用层关系校验”策略：

- schema 保留主键、唯一键和索引
- 不依赖 SQLite 外键生成
- 邮箱、线程、消息之间的归属关系由应用服务与仓储写入逻辑显式校验

### 构建

```bash
dotnet build AgentMailbox.slnx
```

### 测试

```bash
dotnet test AgentMailbox.WebApis.Tests/AgentMailbox.WebApis.Tests.csproj
```

### 运行

```bash
dotnet run --project AgentMailbox.WebApis/AgentMailbox.WebApis.csproj
```

默认配置下：

- SQLite 数据文件位于 `AgentMailbox.WebApis/app_data/agentmailbox.db`
- 原始邮件目录位于 `AgentMailbox.WebApis/app_data/raw-mails`
- 附件目录位于 `AgentMailbox.WebApis/app_data/attachments`
- 入站监听与出站 SMTP 分发默认关闭
- 应用启动时会自动执行 `FluentMigrator` 数据库升级

## 数据库

数据库 schema 由 `FluentMigrator` 迁移类统一管理，应用启动时自动执行升级。

当前首版迁移位于：

- `AgentMailbox.Repositories/Migrations/InitialSqliteSchemaMigration.cs`

`dbscripts/` 不再作为运行时 schema 权威来源，仅保留历史说明用途。

## 文档索引

- [项目总览](/Users/duke/taskpilots/AgentMailbox/docs/project-overview.md)

## 当前边界

- 这是“框架可运行”的起始版本，不是完整可上线的邮件平台。
- 入站 `TcpListener` 与出站 SMTP 仅提供宿主、配置和后台任务占位，不实现完整协议细节。
- 复杂鉴权、线程归并规则、附件处理流程、失败恢复策略将在后续迭代补齐。

## 后续扩展方向

- 实现真实 SMTP 入站接收与 `MimeKit` 解析
- 增强出站发送、失败重试与运维管理
- 增加附件元数据与文件生命周期管理
- 增加更细粒度的 Agent 鉴权与邮箱授权
- 增加搜索、筛选、审计与后台管理界面
