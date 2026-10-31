using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class TechnologyConfiguration
{
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<Technology>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Technology>().HasIndex(x => new { x.Status, x.EnvironmentTested });
        builder.Entity<Resource>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<Hazard>().HasIndex(x => x.Slug).IsUnique();
        builder.Entity<TechnologyFact>().HasIndex(x => new { x.TechnologyId, x.Criterion });
        builder.Entity<MediaAsset>().HasIndex(x => x.NasaId)
            .IsUnique().HasFilter("[NasaId] IS NOT NULL");
        builder.Entity<MediaAsset>().HasIndex(x => new { x.MediaType, x.VerificationStatus });
        builder.Entity<MediaAsset>().ToTable(t =>
        {
            t.HasCheckConstraint("CK_MediaAssets_Dimensions",
                "([Width] IS NULL OR [Width] > 0) AND ([Height] IS NULL OR [Height] > 0)");
            t.HasCheckConstraint("CK_MediaAssets_FileSize", "[FileSizeBytes] IS NULL OR [FileSizeBytes] >= 0");
            t.HasCheckConstraint("CK_MediaAssets_Verification",
                "[VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL)");
        });
        builder.Entity<ArticleMedia>().HasIndex(x => new { x.ArticleId, x.DisplayOrder });
        builder.Entity<MapLayer>().HasIndex(x => new { x.SourceId, x.LayerIdentifier, x.TileMatrixSet }).IsUnique();
    }
}

