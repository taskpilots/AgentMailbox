using FluentMigrator;

namespace AgentMailbox.Repositories.Migrations;

[Migration(202603271600)]
public sealed class InitialSqliteSchemaMigration : Migration
{
    public override void Up()
    {
        if (!Schema.Table("mailboxes").Exists())
        {
            Create.Table("mailboxes")
                .WithColumn("mailbox_id").AsString().PrimaryKey()
                .WithColumn("address").AsString().NotNullable().Unique()
                .WithColumn("display_name").AsString().NotNullable()
                .WithColumn("is_active").AsBoolean().NotNullable()
                .WithColumn("created_at_utc").AsCustom("TEXT").NotNullable();
        }

        if (!Schema.Table("mail_threads").Exists())
        {
            Create.Table("mail_threads")
                .WithColumn("thread_id").AsString().PrimaryKey()
                .WithColumn("subject").AsString().NotNullable()
                .WithColumn("mailbox_id").AsString().NotNullable()
                .WithColumn("created_at_utc").AsCustom("TEXT").NotNullable();
        }

        if (!Schema.Table("mail_messages").Exists())
        {
            Create.Table("mail_messages")
                .WithColumn("message_id").AsString().PrimaryKey()
                .WithColumn("thread_id").AsString().NotNullable()
                .WithColumn("mailbox_id").AsString().NotNullable()
                .WithColumn("direction").AsString().NotNullable()
                .WithColumn("subject").AsString().NotNullable()
                .WithColumn("from_address").AsString().NotNullable()
                .WithColumn("to_address").AsString().NotNullable()
                .WithColumn("body_text").AsString(int.MaxValue).NotNullable()
                .WithColumn("raw_content_path").AsString().Nullable()
                .WithColumn("inbound_status").AsString().Nullable()
                .WithColumn("outbound_status").AsString().Nullable()
                .WithColumn("created_at_utc").AsCustom("TEXT").NotNullable()
                .WithColumn("received_at_utc").AsCustom("TEXT").Nullable()
                .WithColumn("sent_at_utc").AsCustom("TEXT").Nullable()
                .WithColumn("in_reply_to_message_id").AsString().Nullable();
        }

        if (!Schema.Table("outbound_tasks").Exists())
        {
            Create.Table("outbound_tasks")
                .WithColumn("outbound_task_id").AsString().PrimaryKey()
                .WithColumn("message_id").AsString().NotNullable().Unique()
                .WithColumn("status").AsString().NotNullable()
                .WithColumn("attempt_count").AsInt32().NotNullable()
                .WithColumn("next_attempt_at_utc").AsCustom("TEXT").NotNullable()
                .WithColumn("last_error").AsString().Nullable()
                .WithColumn("created_at_utc").AsCustom("TEXT").NotNullable()
                .WithColumn("updated_at_utc").AsCustom("TEXT").NotNullable();
        }

        if (!Schema.Table("mail_messages").Index("idx_mail_messages_mailbox_created_at").Exists())
        {
            Create.Index("idx_mail_messages_mailbox_created_at")
                .OnTable("mail_messages")
                .OnColumn("mailbox_id").Ascending()
                .OnColumn("created_at_utc").Descending();
        }

        if (!Schema.Table("mail_messages").Index("idx_mail_messages_thread_created_at").Exists())
        {
            Create.Index("idx_mail_messages_thread_created_at")
                .OnTable("mail_messages")
                .OnColumn("thread_id").Ascending()
                .OnColumn("created_at_utc").Ascending();
        }

        if (!Schema.Table("outbound_tasks").Index("idx_outbound_tasks_status_next_attempt").Exists())
        {
            Create.Index("idx_outbound_tasks_status_next_attempt")
                .OnTable("outbound_tasks")
                .OnColumn("status").Ascending()
                .OnColumn("next_attempt_at_utc").Ascending();
        }
    }

    public override void Down()
    {
        if (Schema.Table("outbound_tasks").Exists())
        {
            Delete.Table("outbound_tasks");
        }

        if (Schema.Table("mail_messages").Exists())
        {
            Delete.Table("mail_messages");
        }

        if (Schema.Table("mail_threads").Exists())
        {
            Delete.Table("mail_threads");
        }

        if (Schema.Table("mailboxes").Exists())
        {
            Delete.Table("mailboxes");
        }
    }
}
