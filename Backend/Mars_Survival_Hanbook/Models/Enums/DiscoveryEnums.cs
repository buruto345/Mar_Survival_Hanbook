namespace Mars_Survival_Hanbook.Models.Enums;

public enum SourceProvider { ManualLegacy, Ntrs, Images, Pds, NasaWeb, MarsTrek }
public enum IngestionItemStatus { Queued, Processing, Unchanged, RevisionCreated, Rejected, Failed }
public enum ArticleVersionReviewState { Draft, InReview, Approved, ChangesRequested, Rejected, Superseded }
