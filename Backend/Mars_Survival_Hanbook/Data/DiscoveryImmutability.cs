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
                ArticleVersion => ["ReviewState", "ReviewedById", "ReviewedAt", "UpdatedAt", "RowVersion"],
                SourceRevision => ["VerificationStatus", "VerifiedById", "VerifiedAt", "VerificationNotes", "RevokedAt", "RevocationReason", "UpdatedAt", "RowVersion"],
                ArticleVersionSource or PublicationResource or AuditLog => [],
                _ => null
            };
            if (mutable is not null && (e.State == EntityState.Deleted ||
                e.Properties.Any(p => p.IsModified && !mutable.Contains(p.Metadata.Name))))
                throw new InvalidOperationException("Immutable history cannot be edited or deleted; append a new version.");
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
