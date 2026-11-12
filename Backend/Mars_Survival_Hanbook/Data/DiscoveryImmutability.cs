using Mars_Survival_Hanbook.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Mars_Survival_Hanbook.Data;

internal static class DiscoveryImmutability
{
    public static void Validate(ChangeTracker tracker)
    {
        tracker.DetectChanges();
        foreach (var e in tracker.Entries())
        {
            if (e.State is not (EntityState.Modified or EntityState.Deleted)) continue;
            string[]? mutable = e.Entity switch
            {
                LivingScenarioVersion => ["ArchivedAt", "UpdatedAt", "RowVersion"],
                LivingEnglishReview or ScenarioParameter or ScenarioResource or FacilityDefinition or FacilityFlow => [],
                ArticleVersion => ["ReviewState", "ReviewedById", "ReviewedAt", "UpdatedAt", "RowVersion"],
                SourceRevision => ["VerificationStatus", "VerifiedById", "VerifiedAt", "VerificationNotes", "RevokedAt", "RevocationReason", "UpdatedAt", "RowVersion"],
                ScienceTechnologyProfile or ArticleVersionSource or PublicationResource or AuditLog => [],
                _ => null
            };
            if (mutable is not null && (e.State == EntityState.Deleted ||
                e.Properties.Any(p => p.IsModified && !mutable.Contains(p.Metadata.Name))))
                throw new InvalidOperationException("Immutable history cannot be edited or deleted; append a new version.");
        }
        var newScenarios = tracker.Entries<LivingScenarioVersion>().Where(v => v.State == EntityState.Added)
            .Select(v => v.Entity.Id).ToHashSet();
        foreach (var added in tracker.Entries().Where(x => x.State == EntityState.Added))
        {
            Guid? parent = added.Entity switch {
                ScenarioParameter p => p.ScenarioVersionId, ScenarioResource r => r.ScenarioVersionId,
                FacilityDefinition f => f.ScenarioVersionId, FacilityFlow f => f.ScenarioVersionId, _ => null
            };
            if (parent.HasValue && !newScenarios.Contains(parent.Value))
                throw new InvalidOperationException("Scenario configuration must be created with its version.");
        }
        foreach (var e in tracker.Entries<ArticleVersionSource>().Where(x => x.State == EntityState.Added))
        {
            if (!tracker.Entries<ArticleVersion>().Any(v => v.Entity.Id == e.Entity.ArticleVersionId && v.State == EntityState.Added))
                throw new InvalidOperationException("Dependencies must be created with their immutable version.");
        }
        foreach (var e in tracker.Entries<PublicationResource>().Where(x => x.State == EntityState.Added))
            if (!tracker.Entries<ArticleVersion>().Any(v => v.Entity.Id == e.Entity.ArticleVersionId && v.State == EntityState.Added))
                throw new InvalidOperationException("Projection blocks must be created with their immutable version.");
    }
}
