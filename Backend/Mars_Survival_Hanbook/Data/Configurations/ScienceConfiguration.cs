using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class ScienceConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<ScientificSource>().HasIndex(x => x.ExternalRecordId);
        builder.Entity<ScientificSource>().HasIndex(x => x.Doi);
        builder.Entity<ScientificSource>().HasIndex(x => new { x.VerificationStatus, x.NextReviewAt });
        builder.Entity<ScientificSource>().ToTable(t => t.HasCheckConstraint(
            "CK_ScientificSources_Verification",
            "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL)"));
        builder.Entity<Unit>().HasIndex(x => new { x.Symbol, x.QuantityKind }).IsUnique();
        builder.Entity<ScientificFact>().HasIndex(x => new { x.Name, x.EvidenceType });
        builder.Entity<ScientificFact>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_ScientificFacts_Range", "[UpperValue] IS NULL OR [UpperValue] >= [Value]");
            t.HasCheckConstraint("CK_ScientificFacts_RangeKind",
                "[MeasurementKind] <> N'Range' OR [UpperValue] IS NOT NULL");
            t.HasCheckConstraint("CK_ScientificFacts_Verification",
                "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL " +
                "AND [EvidenceType] <> N'Unspecified' AND [TestEnvironment] <> N'Unspecified')");
        });
        builder.Entity<Measurement>().HasIndex(x => new { x.FactId, x.ObservedAt });
        builder.Entity<Measurement>().HasIndex(x => new { x.MissionId, x.LocationId, x.ObservedAt });
        builder.Entity<Measurement>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_Measurements_Range", "[UpperValue] IS NULL OR [UpperValue] >= [Value]");
            t.HasCheckConstraint("CK_Measurements_RangeKind",
                "[MeasurementKind] <> N'Range' OR [UpperValue] IS NOT NULL");
            t.HasCheckConstraint("CK_Measurements_TimeRange",
                "[ObservationEndAt] IS NULL OR [ObservationEndAt] >= [ObservedAt]");
        });
        builder.Entity<ResearchPaper>().HasIndex(x => x.SourceId).IsUnique();
        builder.Entity<ResearchPaper>().HasIndex(x => x.DocumentId).IsUnique();
        builder.Entity<ResearchPaper>().ToTable(t => t.HasCheckConstraint(
            "CK_ResearchPapers_SampleSize", "[SampleSize] IS NULL OR [SampleSize] > 0"));
        builder.Entity<Mission>().HasIndex(x => x.NASAIdentifier).IsUnique();
        builder.Entity<Mission>().HasIndex(x => new { x.Status, x.MissionType });
        builder.Entity<Mission>().ToTable(t => t.HasCheckConstraint(
            "CK_Missions_DateRange", "[EndDate] IS NULL OR [LaunchDate] IS NULL OR [EndDate] >= [LaunchDate]"));
        builder.Entity<MarsLocation>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<MarsLocation>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_MarsLocations_Latitude", "[Latitude] BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_MarsLocations_Longitude", "[Longitude] BETWEEN -180 AND 180");
        });
        builder.Entity<ExplorationEvent>().HasIndex(x => new { x.EventDate, x.IsPlanned });
        builder.Entity<ExplorationEvent>().ToTable(t => t.HasCheckConstraint(
            "CK_ExplorationEvents_DateRange", "[EndDate] IS NULL OR [EndDate] >= [EventDate]"));
    }
}

