using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class OperationsConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<User>().HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Entity<ReviewTask>().HasIndex(x => new { x.ReviewerId, x.State, x.DueAt });
        builder.Entity<ReviewTask>().HasIndex(x => new { x.ArticleId, x.State });
        builder.Entity<ReviewTask>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_ReviewTasks_CompletionDate",
                "[CompletedAt] IS NULL OR [CompletedAt] >= [SubmittedAt]");
            t.HasCheckConstraint("CK_ReviewTasks_Approval",
                "[State] <> N'Approved' OR ([ReviewerId] IS NOT NULL AND [CompletedAt] IS NOT NULL " +
                "AND [SourcesChecked] = 1 AND [ClaimsChecked] = 1 AND [UnitsChecked] = 1 " +
                "AND [EvidenceChecked] = 1 AND [MediaRightsChecked] = 1)");
        });
        builder.Entity<NasaConnector>().HasIndex(x => x.Key).IsUnique();
        builder.Entity<NasaConnector>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_NasaConnectors_RateLimit",
                "[RateLimitPerMinute] IS NULL OR [RateLimitPerMinute] > 0");
            t.HasCheckConstraint("CK_NasaConnectors_Configuration",
                "[ConfigurationJson] IS NULL OR ISJSON([ConfigurationJson]) = 1");
        });
        builder.Entity<IngestionJob>().HasIndex(x => new { x.ConnectorId, x.Status, x.CreatedAt });
        builder.Entity<IngestionJob>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_IngestionJobs_Counts",
                "[AttemptCount] >= 0 AND [RecordsFetched] >= 0 AND [RecordsAccepted] >= 0 " +
                "AND [RecordsRejected] >= 0 AND [RecordsAccepted] + [RecordsRejected] <= [RecordsFetched]");
            t.HasCheckConstraint("CK_IngestionJobs_TimeRange",
                "[CompletedAt] IS NULL OR ([StartedAt] IS NOT NULL AND [CompletedAt] >= [StartedAt])");
        });
        builder.Entity<AuditLog>().HasIndex(x => new { x.EntityType, x.EntityId, x.OccurredAt });
        builder.Entity<AuditLog>().HasIndex(x => x.OccurredAt);
        builder.Entity<AuditLog>().ToTable(t => t.HasCheckConstraint(
            "CK_AuditLogs_Metadata", "[MetadataJson] IS NULL OR ISJSON([MetadataJson]) = 1"));
        builder.Entity<FeedbackReport>().HasIndex(x => new { x.Status, x.CreatedAt });
        builder.Entity<FeedbackReport>().ToTable(t => t.HasCheckConstraint(
            "CK_FeedbackReports_Resolution",
            "[Status] <> N'Resolved' OR ([ResolvedAt] IS NOT NULL AND [ResolvedById] IS NOT NULL)"));
        builder.Entity<Bookmark>().HasIndex(x => new { x.UserId, x.CreatedAt });
    }
}

