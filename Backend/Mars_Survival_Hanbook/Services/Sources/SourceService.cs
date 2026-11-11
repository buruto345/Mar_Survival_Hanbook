using System.Text;
using Mars_Survival_Hanbook.Contracts;
using Mars_Survival_Hanbook.Data;
using Mars_Survival_Hanbook.Models;
using Mars_Survival_Hanbook.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Mars_Survival_Hanbook.Services.Sources;

public record SourceCandidate(SourceProvider Provider, string ExternalId, string? ProviderVersion,
    SourceMetadata Metadata, string RawPath, string RawHash, DateTimeOffset RetrievedAt);
public sealed class SourceService(MarsDbContext db, CurrentActor actor, RawStorage raw)
{
    public async Task<(SourceRevision Revision, bool Created)> ManualAsync(ManualSourceRequest request, CancellationToken ct)
    {
        _ = SourceNormalization.CanonicalUrl(request.Url);
        if (request.RetrievedAt == default || request.RetrievedAt > DateTimeOffset.UtcNow.AddMinutes(5))
            throw ApiException.Invalid("The retrieval date is invalid.");
        var metadata = new SourceMetadata(request.Title, request.Url, request.Authors, request.Organization,
            request.SourceType, request.Excerpt, request.UsageNote, request.Credit);
        var stored = await raw.PutAsync(Encoding.UTF8.GetBytes(DiscoveryJson.Write(metadata)), ct);
        return await StoreAsync(new(SourceProvider.NasaWeb, request.Url, null, metadata,
            stored.Path, stored.Hash, request.RetrievedAt), null, actor.Id, ct);
    }
    public async Task<(SourceRevision Revision, bool Created)> StoreAsync(
        SourceCandidate candidate, IngestionItem? item, Guid? actorId, CancellationToken ct)
    {
        var identity = SourceNormalization.Identity(candidate.Provider, candidate.ExternalId, candidate.Metadata.Url);
        if (string.IsNullOrWhiteSpace(candidate.Metadata.Title) || candidate.Metadata.Title.Length > 500 ||
            candidate.Metadata.Url.Length > 2048 || candidate.Metadata.Organization.Length > 300)
            throw new Mars_Survival_Hanbook.Integrations.Nasa.UpstreamException("invalid_metadata_length", false);
        var (contentHash, fingerprint) = SourceNormalization.Fingerprint(candidate.Metadata, candidate.ProviderVersion);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "source:" + SourceNormalization.Hash(candidate.Provider + ":" + identity), ct);
        var source = await db.ScientificSources.SingleOrDefaultAsync(x => x.Provider == candidate.Provider && x.IdentityKey == identity, ct);
        if (source is null)
        {
            source = new ScientificSource { Provider = candidate.Provider, IdentityKey = identity,
                Title = candidate.Metadata.Title, NASAOrganization = candidate.Metadata.Organization,
                Url = candidate.Metadata.Url, CanonicalUrl = SourceNormalization.CanonicalUrl(candidate.Metadata.Url),
                ExternalRecordId = candidate.ExternalId.Length <= 200 ? candidate.ExternalId : null,
                SourceType = candidate.Metadata.SourceType, RetrievedAt = candidate.RetrievedAt };
            db.ScientificSources.Add(source);
            await db.SaveChangesAsync(ct);
        }
        var revision = await db.SourceRevisions.SingleOrDefaultAsync(x => x.SourceId == source.Id && x.Fingerprint == fingerprint, ct);
        var created = revision is null;
        if (revision is null)
        {
            revision = new SourceRevision { SourceId = source.Id,
                RevisionNumber = (await db.SourceRevisions.Where(x => x.SourceId == source.Id).MaxAsync(x => (int?)x.RevisionNumber, ct) ?? 0) + 1,
                ContentHash = contentHash, Fingerprint = fingerprint, ProviderVersion = candidate.ProviderVersion,
                MetadataSnapshot = DiscoveryJson.Write(candidate.Metadata), RawStoragePath = candidate.RawPath,
                RawChecksum = candidate.RawHash, RetrievedAt = candidate.RetrievedAt, IngestionJobId = item?.JobId };
            db.SourceRevisions.Add(revision);
            await Mars_Survival_Hanbook.Services.Ingestion.ImportedContentMapper.StageAsync(db, source, revision, candidate.Metadata, ct);
            var affected = await db.Articles.Where(a => db.ArticleVersionSources.Any(link =>
                link.SourceId == source.Id && link.SourceRevisionId != revision.Id &&
                (link.ArticleVersionId == a.PublishedVersionId || link.ArticleVersionId == a.CurrentDraftVersionId))).ToListAsync(ct);
            foreach (var a in affected) a.RequiresReview = true;
            Audit.Add(db, actorId, nameof(SourceRevision), revision.Id, AuditAction.Created,
                "Created pending source revision " + revision.RevisionNumber, actor.TraceId);
        }
        source.LastCheckedAt = DateTimeOffset.UtcNow;
        if (item is not null)
        {
            item.SourceId = source.Id; item.SourceRevisionId = revision.Id; item.IdentityKey = identity;
            item.RawStoragePath = candidate.RawPath; item.RawChecksum = candidate.RawHash;
            item.NormalizedPayload = revision.MetadataSnapshot;
            item.Status = created ? IngestionItemStatus.RevisionCreated : IngestionItemStatus.Unchanged;
            item.CompletedAt = DateTimeOffset.UtcNow; item.ErrorCode = null; item.ErrorMessage = null; item.NextAttemptAt = null;
        }
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return (revision, created);
    }
    public async Task<(SourceRevision Revision, bool Created)> AmendAsync(Guid id, SourceAmendmentRequest request, string? etag, CancellationToken ct)
    {
        var previous = await db.SourceRevisions.Include(x => x.Source).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        Concurrency.Check(previous, etag);
        if (previous.RawStoragePath is null || previous.RawChecksum is null) throw ApiException.Gate("Archive the original evidence before amending its metadata.");
        var m = DiscoveryJson.Read<SourceMetadata>(previous.MetadataSnapshot);
        var metadata = m with { Title = request.Title, Authors = request.Authors, Organization = request.Organization,
            Excerpt = request.Excerpt, UsageNote = request.UsageNote, Credit = request.Credit };
        var external = m.ExternalRecordId ?? previous.Source.ExternalRecordId ??
            (previous.Source.Provider == SourceProvider.NasaWeb ? m.Url : previous.Source.Id.ToString());
        return await StoreAsync(new(previous.Source.Provider, external, previous.ProviderVersion, metadata,
            previous.RawStoragePath, previous.RawChecksum, previous.RetrievedAt), null, actor.Id, ct);
    }
    public async Task<SourceRevision> DecideAsync(Guid id, SourceDecision request, string? etag, CancellationToken ct)
    {
        if (request.Decision is not (VerificationStatus.Accepted or VerificationStatus.Rejected))
            throw ApiException.Invalid("Choose Accepted or Rejected.");
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "publication", ct);
        var r = await db.SourceRevisions.Include(x => x.Source).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        Concurrency.Check(r, etag);
        if (r.RevokedAt != null || r.VerificationStatus != VerificationStatus.Pending)
            throw ApiException.Conflict("This revision already has a decision. Use revocation to withdraw an accepted revision.");
        var m = DiscoveryJson.Read<SourceMetadata>(r.MetadataSnapshot);
        if (request.Decision == VerificationStatus.Accepted &&
            (m.Authors.Length == 0 || m.Authors.Any(a => string.IsNullOrWhiteSpace(a.Name) || string.IsNullOrWhiteSpace(a.Organization)) ||
             string.IsNullOrWhiteSpace(m.Organization) || string.IsNullOrWhiteSpace(request.Notes) || r.RawStoragePath is null))
            throw ApiException.Gate("Approval requires author affiliations, a responsible organization, archived evidence and review notes.");
        r.VerificationStatus = request.Decision; r.VerificationNotes = request.Notes;
        r.VerifiedById = actor.Id; r.VerifiedAt = DateTimeOffset.UtcNow;
        if (request.Decision == VerificationStatus.Accepted)
        {
            var s = r.Source; s.CurrentAcceptedRevisionId = r.Id; s.Title = m.Title; s.Url = m.Url;
            var authorSummary = string.Join("; ", m.Authors.Select(a => a.Name));
            s.Authors = authorSummary.Length <= 2000 ? authorSummary : null;
            s.VerificationStatus = VerificationStatus.Accepted; s.VerifiedById = actor.Id; s.VerifiedAt = r.VerifiedAt;
            s.VerificationNotes = request.Notes; s.NASAOrganization = m.Organization;
        }
        Audit.Add(db, actor.Id, nameof(SourceRevision), id,
            request.Decision == VerificationStatus.Accepted ? AuditAction.Approved : AuditAction.Rejected, request.Notes, actor.TraceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return r;
    }
    public async Task<SourceRevision> RevokeAsync(Guid id, string reason, string? etag, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await DatabaseLocks.AcquireAsync(db, "publication", ct);
        var r = await db.SourceRevisions.Include(x => x.Source).SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw ApiException.Missing();
        Concurrency.Check(r, etag);
        if (r.VerificationStatus != VerificationStatus.Accepted || r.RevokedAt != null)
            throw ApiException.Conflict("Only an active accepted revision can be revoked.");
        r.RevokedAt = DateTimeOffset.UtcNow; r.RevocationReason = reason;
        if (r.Source.CurrentAcceptedRevisionId == id) r.Source.CurrentAcceptedRevisionId = null;
        foreach (var a in await db.Articles.Where(a => db.ArticleVersionSources.Any(l =>
            l.SourceRevisionId == id && (l.ArticleVersionId == a.PublishedVersionId || l.ArticleVersionId == a.CurrentDraftVersionId))).ToListAsync(ct))
            a.RequiresReview = true;
        Audit.Add(db, actor.Id, nameof(SourceRevision), id, AuditAction.Archived, reason, actor.TraceId);
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return r;
    }
}
