using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class DiscoveryConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        var s = b.Entity<ScientificSource>();
        s.Property(x => x.IdentityKey).UseCollation("Latin1_General_100_BIN2");
        s.HasIndex(x => new { x.Provider, x.IdentityKey }).IsUnique();
        s.HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.CurrentAcceptedRevisionId, x.Id })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        var r = b.Entity<SourceRevision>();
        r.ToTable("SourceRevisions", t => {
            t.HasCheckConstraint("CK_SourceRevisions_Number", "[RevisionNumber] > 0");
            t.HasCheckConstraint("CK_SourceRevisions_Metadata", "ISJSON([MetadataSnapshot]) = 1");
            t.HasCheckConstraint("CK_SourceRevisions_Verification", "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL)");
            t.HasCheckConstraint("CK_SourceRevisions_Revocation", "[RevokedAt] IS NULL OR [RevocationReason] IS NOT NULL");
        });
        r.HasAlternateKey(x => new { x.Id, x.SourceId });
        r.HasIndex(x => new { x.SourceId, x.RevisionNumber }).IsUnique();
        r.HasIndex(x => new { x.SourceId, x.Fingerprint }).IsUnique();
        r.HasOne(x => x.Source).WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.NoAction);
        r.HasOne<User>().WithMany().HasForeignKey(x => x.VerifiedById).OnDelete(DeleteBehavior.NoAction);
        r.HasOne<IngestionJob>().WithMany().HasForeignKey(x => x.IngestionJobId).OnDelete(DeleteBehavior.NoAction);
        var a = b.Entity<Article>();
        a.ToTable(t => t.HasCheckConstraint("CK_Articles_PublicationPointer", "[Status] <> N'Published' OR [PublishedVersionId] IS NOT NULL"));
        a.HasOne<ArticleVersion>().WithMany().HasForeignKey(x => new { x.PublishedVersionId, x.Id })
            .HasPrincipalKey(x => new { x.Id, x.ArticleId }).OnDelete(DeleteBehavior.NoAction);
        a.HasOne<ArticleVersion>().WithMany().HasForeignKey(x => new { x.CurrentDraftVersionId, x.Id })
            .HasPrincipalKey(x => new { x.Id, x.ArticleId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleVersion>().HasOne<User>().WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleVersion>().HasIndex(x => new { x.CategorySlug, x.LanguageCode });
        var avs = b.Entity<ArticleVersionSource>();
        avs.ToTable("ArticleVersionSources");
        avs.HasKey(x => new { x.ArticleVersionId, x.SourceRevisionId });
        avs.HasOne<ArticleVersion>().WithMany().HasForeignKey(x => x.ArticleVersionId).OnDelete(DeleteBehavior.NoAction);
        avs.HasOne(x => x.SourceRevision).WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        var j = b.Entity<IngestionJob>();
        j.HasIndex(x => new { x.RequestedById, x.ConnectorId, x.RequestKey }).IsUnique()
            .HasFilter("[RequestKey] IS NOT NULL AND [RequestedById] IS NOT NULL");
        j.HasIndex(x => new { x.Status, x.NextAttemptAt, x.LeaseExpiresAt });
        j.ToTable(t => t.HasCheckConstraint("CK_IngestionJobs_DetailedCounts",
            "[RecordsUnchanged] >= 0 AND [RecordsFailed] >= 0 AND [RecordsAccepted] + [RecordsRejected] + [RecordsUnchanged] + [RecordsFailed] <= [RecordsFetched]"));
        var i = b.Entity<IngestionItem>();
        i.ToTable("IngestionItems");
        i.Property(x => x.ItemKey).UseCollation("Latin1_General_100_BIN2");
        i.HasIndex(x => new { x.JobId, x.ItemKey }).IsUnique();
        i.HasOne<IngestionJob>().WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.NoAction);
        i.HasOne<ScientificSource>().WithMany().HasForeignKey(x => x.SourceId).OnDelete(DeleteBehavior.NoAction);
        i.HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        var p = b.Entity<PublicationResource>();
        p.ToTable("PublicationResources", t => t.HasCheckConstraint("CK_PublicationResources_Json", "ISJSON([Payload]) = 1"));
        p.HasKey(x => new { x.ArticleVersionId, x.Kind, x.ResourceId });
        p.HasOne<ArticleVersion>().WithMany().HasForeignKey(x => x.ArticleVersionId).OnDelete(DeleteBehavior.NoAction);
        p.HasIndex(x => new { x.Kind, x.ResourceId });
        p.HasIndex(x => new { x.Kind, x.Date });
        p.Property(x => x.Latitude).HasPrecision(10, 7);
        p.Property(x => x.Longitude).HasPrecision(10, 7);
        b.Entity<MapLayer>().HasOne<MarsLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Measurement>().HasIndex(x => new { x.SourceRevisionId, x.ExternalObservationKey })
            .IsUnique().HasFilter("[SourceRevisionId] IS NOT NULL AND [ExternalObservationKey] IS NOT NULL");
        b.Entity<Measurement>().Property(x => x.ExternalObservationKey).UseCollation("Latin1_General_100_BIN2");
        b.Entity<ScientificFact>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Measurement>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<MediaAsset>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<Mission>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<MarsLocation>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ExplorationEvent>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<MapLayer>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleSource>().HasOne<SourceRevision>().WithMany().HasForeignKey(x => new { x.SourceRevisionId, x.SourceId })
            .HasPrincipalKey(x => new { x.Id, x.SourceId }).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleMission>().ToTable("ArticleMissions");
        b.Entity<ArticleMission>().HasKey(x => new { x.ArticleId, x.MissionId });
        b.Entity<ArticleMission>().HasOne<Article>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleMission>().HasOne<Mission>().WithMany().HasForeignKey(x => x.MissionId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleLocation>().ToTable("ArticleLocations");
        b.Entity<ArticleLocation>().HasKey(x => new { x.ArticleId, x.LocationId });
        b.Entity<ArticleLocation>().HasOne<Article>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleLocation>().HasOne<MarsLocation>().WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleExplorationEvent>().ToTable("ArticleExplorationEvents");
        b.Entity<ArticleExplorationEvent>().HasKey(x => new { x.ArticleId, x.ExplorationEventId });
        b.Entity<ArticleExplorationEvent>().HasOne<Article>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleExplorationEvent>().HasOne<ExplorationEvent>().WithMany().HasForeignKey(x => x.ExplorationEventId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleMapLayer>().ToTable("ArticleMapLayers");
        b.Entity<ArticleMapLayer>().HasKey(x => new { x.ArticleId, x.MapLayerId });
        b.Entity<ArticleMapLayer>().HasOne<Article>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleMapLayer>().HasOne<MapLayer>().WithMany().HasForeignKey(x => x.MapLayerId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleMeasurement>().ToTable("ArticleMeasurements");
        b.Entity<ArticleMeasurement>().HasKey(x => new { x.ArticleId, x.MeasurementId });
        b.Entity<ArticleMeasurement>().HasOne<Article>().WithMany().HasForeignKey(x => x.ArticleId).OnDelete(DeleteBehavior.NoAction);
        b.Entity<ArticleMeasurement>().HasOne<Measurement>().WithMany().HasForeignKey(x => x.MeasurementId).OnDelete(DeleteBehavior.NoAction);
    }
}
