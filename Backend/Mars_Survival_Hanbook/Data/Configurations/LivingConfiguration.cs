using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Data.Configurations;

internal static class LivingConfiguration
{
    public static void Configure(ModelBuilder b)
    {
        var version = b.Entity<LivingScenarioVersion>();
        version.HasIndex(x => new { x.ScenarioKey, x.Version }).IsUnique();
        version.HasMany(x => x.Parameters).WithOne().HasForeignKey(x => x.ScenarioVersionId);
        version.HasMany(x => x.Resources).WithOne().HasForeignKey(x => x.ScenarioVersionId);
        version.HasMany(x => x.Facilities).WithOne().HasForeignKey(x => x.ScenarioVersionId);
        var p = b.Entity<ScenarioParameter>();
        p.HasKey(x => new { x.ScenarioVersionId, x.Key });
        p.Property(x => x.OriginalValue).HasPrecision(24, 12);
        p.Property(x => x.NormalizedValue).HasPrecision(24, 12);
        p.ToTable(t => t.HasCheckConstraint("CK_LivingParameter_Value", "[OriginalValue]>=0 AND [NormalizedValue]>=0"));
        var r = b.Entity<ScenarioResource>();
        r.HasKey(x => new { x.ScenarioVersionId, x.ResourceId });
        r.Property(x => x.InitialQuantity).HasPrecision(24, 12);
        r.Property(x => x.Capacity).HasPrecision(24, 12);
        r.ToTable(t => t.HasCheckConstraint("CK_LivingStock_Range", "[InitialQuantity]>=0 AND [Capacity]>=[InitialQuantity]"));
        var f = b.Entity<FacilityDefinition>();
        f.HasKey(x => new { x.ScenarioVersionId, x.TechnologyId });
        f.ToTable(t => t.HasCheckConstraint("CK_LivingFacility_Count", "[DefaultCount]>=0 AND [MaxCount]>=[DefaultCount]"));
        var flow = b.Entity<FacilityFlow>();
        flow.HasKey(x => new { x.ScenarioVersionId, x.TechnologyId, x.ResourceId, x.Phase, x.Direction });
        flow.HasOne<FacilityDefinition>().WithMany().HasForeignKey(x => new { x.ScenarioVersionId, x.TechnologyId });
        flow.HasOne<ScenarioResource>().WithMany().HasForeignKey(x => new { x.ScenarioVersionId, x.ResourceId });
        flow.HasOne<ScenarioParameter>().WithMany().HasForeignKey(x => new { x.ScenarioVersionId, x.ParameterKey });
        r.HasOne(x => x.Unit).WithMany().HasForeignKey(x => x.UnitId);
        b.Entity<LivingEnglishReview>().HasKey(x => x.ArticleVersionId);
        b.Entity<LivingEnglishReview>().HasOne(x => x.ArticleVersion).WithMany().HasForeignKey(x => x.ArticleVersionId);
        foreach (var entity in new[] { p.Metadata, r.Metadata, f.Metadata, flow.Metadata, b.Entity<LivingEnglishReview>().Metadata })
            foreach (var fk in entity.GetForeignKeys()) fk.DeleteBehavior = DeleteBehavior.NoAction;
    }
}
