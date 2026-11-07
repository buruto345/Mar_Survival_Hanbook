BEGIN TRANSACTION;
GO

DROP INDEX [IX_IngestionJobs_RequestedById] ON [IngestionJobs];
GO

ALTER TABLE [ScientificSources] ADD [CurrentAcceptedRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [ScientificSources] ADD [IdentityKey] nvarchar(450) COLLATE Latin1_General_100_BIN2 NULL;
GO

ALTER TABLE [ScientificSources] ADD [LastCheckedAt] datetimeoffset NULL;
GO

ALTER TABLE [ScientificSources] ADD [Provider] nvarchar(48) NOT NULL DEFAULT N'ManualLegacy';
GO

UPDATE ScientificSources
SET IdentityKey = N'legacy:' + LOWER(CONVERT(nvarchar(36), Id))
WHERE IdentityKey IS NULL;
GO

ALTER TABLE [ScientificFacts] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [Missions] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [MediaAssets] ADD [FileChecksum] nvarchar(64) NULL;
GO

ALTER TABLE [MediaAssets] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [Measurements] ADD [ExternalObservationKey] nvarchar(450) NULL;
GO

ALTER TABLE [Measurements] ADD [QualityFlag] nvarchar(100) NULL;
GO

ALTER TABLE [Measurements] ADD [QualityNotes] nvarchar(2000) NULL;
GO

ALTER TABLE [Measurements] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [MarsLocations] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [MapLayers] ADD [BoundingBoxJson] nvarchar(max) NULL;
GO

ALTER TABLE [MapLayers] ADD [CapabilitiesStoragePath] nvarchar(2048) NULL;
GO

ALTER TABLE [MapLayers] ADD [CoordinateSystem] nvarchar(150) NULL;
GO

ALTER TABLE [MapLayers] ADD [LocationId] uniqueidentifier NULL;
GO

ALTER TABLE [MapLayers] ADD [ResourceUrlTemplate] nvarchar(2048) NULL;
GO

ALTER TABLE [MapLayers] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [MapLayers] ADD [Style] nvarchar(200) NULL;
GO

ALTER TABLE [IngestionJobs] ADD [CheckpointJson] nvarchar(max) NULL;
GO

ALTER TABLE [IngestionJobs] ADD [LeaseExpiresAt] datetimeoffset NULL;
GO

ALTER TABLE [IngestionJobs] ADD [LeaseOwner] nvarchar(100) NULL;
GO

ALTER TABLE [IngestionJobs] ADD [NextAttemptAt] datetimeoffset NULL;
GO

ALTER TABLE [IngestionJobs] ADD [RecordsFailed] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [IngestionJobs] ADD [RecordsUnchanged] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [IngestionJobs] ADD [RequestHash] nvarchar(64) NULL;
GO

ALTER TABLE [IngestionJobs] ADD [RequestJson] nvarchar(max) NULL;
GO

ALTER TABLE [IngestionJobs] ADD [RequestKey] nvarchar(200) NULL;
GO

ALTER TABLE [ExplorationEvents] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [ArticleVersions] ADD [CategorySlug] nvarchar(180) NOT NULL DEFAULT N'';
GO

ALTER TABLE [ArticleVersions] ADD [LanguageCode] nvarchar(10) NOT NULL DEFAULT N'';
GO

ALTER TABLE [ArticleVersions] ADD [ReviewState] nvarchar(48) NOT NULL DEFAULT N'Draft';
GO

ALTER TABLE [ArticleVersions] ADD [ReviewedAt] datetimeoffset NULL;
GO

ALTER TABLE [ArticleVersions] ADD [ReviewedById] uniqueidentifier NULL;
GO

ALTER TABLE [ArticleVersions] ADD [SearchText] nvarchar(max) NOT NULL DEFAULT N'';
GO

ALTER TABLE [ArticleVersions] ADD [SnapshotHash] nvarchar(64) NOT NULL DEFAULT N'';
GO

ALTER TABLE [ArticleVersions] ADD [SnapshotSchemaVersion] int NOT NULL DEFAULT 1;
GO

ALTER TABLE [ArticleVersions] ADD [Summary] nvarchar(2000) NOT NULL DEFAULT N'';
GO

ALTER TABLE [ArticleVersions] ADD [Title] nvarchar(300) NOT NULL DEFAULT N'';
GO

ALTER TABLE [ArticleSources] ADD [SourceRevisionId] uniqueidentifier NULL;
GO

ALTER TABLE [Articles] ADD [CurrentDraftVersionId] uniqueidentifier NULL;
GO

ALTER TABLE [Articles] ADD [PublishedVersionId] uniqueidentifier NULL;
GO

ALTER TABLE [Articles] ADD [RequiresReview] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

CREATE TABLE [ArticleExplorationEvents] (
    [ArticleId] uniqueidentifier NOT NULL,
    [ExplorationEventId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArticleExplorationEvents] PRIMARY KEY ([ArticleId], [ExplorationEventId]),
    CONSTRAINT [FK_ArticleExplorationEvents_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]),
    CONSTRAINT [FK_ArticleExplorationEvents_ExplorationEvents_ExplorationEventId] FOREIGN KEY ([ExplorationEventId]) REFERENCES [ExplorationEvents] ([Id])
);
GO

CREATE TABLE [ArticleLocations] (
    [ArticleId] uniqueidentifier NOT NULL,
    [LocationId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArticleLocations] PRIMARY KEY ([ArticleId], [LocationId]),
    CONSTRAINT [FK_ArticleLocations_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]),
    CONSTRAINT [FK_ArticleLocations_MarsLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [MarsLocations] ([Id])
);
GO

CREATE TABLE [ArticleMapLayers] (
    [ArticleId] uniqueidentifier NOT NULL,
    [MapLayerId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArticleMapLayers] PRIMARY KEY ([ArticleId], [MapLayerId]),
    CONSTRAINT [FK_ArticleMapLayers_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]),
    CONSTRAINT [FK_ArticleMapLayers_MapLayers_MapLayerId] FOREIGN KEY ([MapLayerId]) REFERENCES [MapLayers] ([Id])
);
GO

CREATE TABLE [ArticleMeasurements] (
    [ArticleId] uniqueidentifier NOT NULL,
    [MeasurementId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArticleMeasurements] PRIMARY KEY ([ArticleId], [MeasurementId]),
    CONSTRAINT [FK_ArticleMeasurements_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]),
    CONSTRAINT [FK_ArticleMeasurements_Measurements_MeasurementId] FOREIGN KEY ([MeasurementId]) REFERENCES [Measurements] ([Id])
);
GO

CREATE TABLE [ArticleMissions] (
    [ArticleId] uniqueidentifier NOT NULL,
    [MissionId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArticleMissions] PRIMARY KEY ([ArticleId], [MissionId]),
    CONSTRAINT [FK_ArticleMissions_Articles_ArticleId] FOREIGN KEY ([ArticleId]) REFERENCES [Articles] ([Id]),
    CONSTRAINT [FK_ArticleMissions_Missions_MissionId] FOREIGN KEY ([MissionId]) REFERENCES [Missions] ([Id])
);
GO

CREATE TABLE [PublicationResources] (
    [ArticleVersionId] uniqueidentifier NOT NULL,
    [Kind] nvarchar(32) NOT NULL,
    [ResourceId] uniqueidentifier NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [Name] nvarchar(500) NOT NULL,
    [Status] nvarchar(100) NULL,
    [Type] nvarchar(100) NULL,
    [QuantityKind] nvarchar(100) NULL,
    [EvidenceType] nvarchar(48) NULL,
    [MissionId] uniqueidentifier NULL,
    [LocationId] uniqueidentifier NULL,
    [FactId] uniqueidentifier NULL,
    [Date] datetimeoffset NULL,
    [IsPlanned] bit NOT NULL,
    [Latitude] decimal(10,7) NULL,
    [Longitude] decimal(10,7) NULL,
    CONSTRAINT [PK_PublicationResources] PRIMARY KEY ([ArticleVersionId], [Kind], [ResourceId]),
    CONSTRAINT [CK_PublicationResources_Json] CHECK (ISJSON([Payload]) = 1),
    CONSTRAINT [FK_PublicationResources_ArticleVersions_ArticleVersionId] FOREIGN KEY ([ArticleVersionId]) REFERENCES [ArticleVersions] ([Id])
);
GO

CREATE TABLE [SourceRevisions] (
    [Id] uniqueidentifier NOT NULL,
    [SourceId] uniqueidentifier NOT NULL,
    [RevisionNumber] int NOT NULL,
    [ProviderVersion] nvarchar(200) NULL,
    [ContentHash] nvarchar(64) NOT NULL,
    [Fingerprint] nvarchar(64) NOT NULL,
    [NormalizationVersion] nvarchar(100) NOT NULL,
    [MetadataSnapshot] nvarchar(max) NOT NULL,
    [RawStoragePath] nvarchar(2048) NULL,
    [RawChecksum] nvarchar(64) NULL,
    [IngestionJobId] uniqueidentifier NULL,
    [RetrievedAt] datetimeoffset NOT NULL,
    [VerificationStatus] nvarchar(48) NOT NULL,
    [VerifiedById] uniqueidentifier NULL,
    [VerifiedAt] datetimeoffset NULL,
    [VerificationNotes] nvarchar(4000) NULL,
    [RevokedAt] datetimeoffset NULL,
    [RevocationReason] nvarchar(4000) NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [ArchivedAt] datetimeoffset NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_SourceRevisions] PRIMARY KEY ([Id]),
    CONSTRAINT [AK_SourceRevisions_Id_SourceId] UNIQUE ([Id], [SourceId]),
    CONSTRAINT [CK_SourceRevisions_Metadata] CHECK (ISJSON([MetadataSnapshot]) = 1),
    CONSTRAINT [CK_SourceRevisions_Number] CHECK ([RevisionNumber] > 0),
    CONSTRAINT [CK_SourceRevisions_Revocation] CHECK ([RevokedAt] IS NULL OR [RevocationReason] IS NOT NULL),
    CONSTRAINT [CK_SourceRevisions_Verification] CHECK ([VerificationStatus] <> N'Accepted' OR ([VerifiedAt] IS NOT NULL AND [VerifiedById] IS NOT NULL)),
    CONSTRAINT [CK_SourceRevisions_VerificationStatus] CHECK ([VerificationStatus] IN (N'Pending', N'Accepted', N'Rejected')),
    CONSTRAINT [FK_SourceRevisions_IngestionJobs_IngestionJobId] FOREIGN KEY ([IngestionJobId]) REFERENCES [IngestionJobs] ([Id]),
    CONSTRAINT [FK_SourceRevisions_ScientificSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [ScientificSources] ([Id]),
    CONSTRAINT [FK_SourceRevisions_Users_VerifiedById] FOREIGN KEY ([VerifiedById]) REFERENCES [Users] ([Id])
);
GO

CREATE TABLE [ArticleVersionSources] (
    [ArticleVersionId] uniqueidentifier NOT NULL,
    [SourceRevisionId] uniqueidentifier NOT NULL,
    [SourceId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_ArticleVersionSources] PRIMARY KEY ([ArticleVersionId], [SourceRevisionId]),
    CONSTRAINT [FK_ArticleVersionSources_ArticleVersions_ArticleVersionId] FOREIGN KEY ([ArticleVersionId]) REFERENCES [ArticleVersions] ([Id]),
    CONSTRAINT [FK_ArticleVersionSources_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId])
);
GO

CREATE TABLE [IngestionItems] (
    [Id] uniqueidentifier NOT NULL,
    [JobId] uniqueidentifier NOT NULL,
    [ItemKey] nvarchar(450) NOT NULL,
    [Provider] nvarchar(48) NOT NULL,
    [IdentityKey] nvarchar(450) NULL,
    [RawPayload] nvarchar(max) NULL,
    [RawStoragePath] nvarchar(2048) NULL,
    [RawChecksum] nvarchar(64) NULL,
    [NormalizedPayload] nvarchar(max) NULL,
    [NormalizationVersion] nvarchar(100) NOT NULL,
    [SourceId] uniqueidentifier NULL,
    [SourceRevisionId] uniqueidentifier NULL,
    [Status] nvarchar(48) NOT NULL,
    [ErrorCode] nvarchar(100) NULL,
    [ErrorMessage] nvarchar(2000) NULL,
    [AttemptCount] int NOT NULL,
    [NextAttemptAt] datetimeoffset NULL,
    [CompletedAt] datetimeoffset NULL,
    [CreatedAt] datetimeoffset NOT NULL,
    [UpdatedAt] datetimeoffset NULL,
    [ArchivedAt] datetimeoffset NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_IngestionItems] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_IngestionItems_Provider] CHECK ([Provider] IN (N'ManualLegacy', N'Ntrs', N'Images', N'Pds', N'NasaWeb', N'MarsTrek')),
    CONSTRAINT [CK_IngestionItems_Status] CHECK ([Status] IN (N'Queued', N'Processing', N'Unchanged', N'RevisionCreated', N'Rejected', N'Failed')),
    CONSTRAINT [FK_IngestionItems_IngestionJobs_JobId] FOREIGN KEY ([JobId]) REFERENCES [IngestionJobs] ([Id]),
    CONSTRAINT [FK_IngestionItems_ScientificSources_SourceId] FOREIGN KEY ([SourceId]) REFERENCES [ScientificSources] ([Id]),
    CONSTRAINT [FK_IngestionItems_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId])
);
GO

CREATE INDEX [IX_ScientificSources_CurrentAcceptedRevisionId_Id] ON [ScientificSources] ([CurrentAcceptedRevisionId], [Id]);
GO

ALTER TABLE [ScientificSources] ADD CONSTRAINT [CK_ScientificSources_Provider] CHECK ([Provider] IN (N'ManualLegacy', N'Ntrs', N'Images', N'Pds', N'NasaWeb', N'MarsTrek'));
GO

CREATE INDEX [IX_ScientificFacts_SourceRevisionId_SourceId] ON [ScientificFacts] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_Missions_SourceRevisionId_SourceId] ON [Missions] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_MediaAssets_SourceRevisionId_SourceId] ON [MediaAssets] ([SourceRevisionId], [SourceId]);
GO

CREATE UNIQUE INDEX [IX_Measurements_SourceRevisionId_ExternalObservationKey] ON [Measurements] ([SourceRevisionId], [ExternalObservationKey]) WHERE [SourceRevisionId] IS NOT NULL AND [ExternalObservationKey] IS NOT NULL;
GO

CREATE INDEX [IX_Measurements_SourceRevisionId_SourceId] ON [Measurements] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_MarsLocations_SourceRevisionId_SourceId] ON [MarsLocations] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_MapLayers_LocationId] ON [MapLayers] ([LocationId]);
GO

CREATE INDEX [IX_MapLayers_SourceRevisionId_SourceId] ON [MapLayers] ([SourceRevisionId], [SourceId]);
GO

CREATE UNIQUE INDEX [IX_IngestionJobs_RequestedById_ConnectorId_RequestKey] ON [IngestionJobs] ([RequestedById], [ConnectorId], [RequestKey]) WHERE [RequestKey] IS NOT NULL AND [RequestedById] IS NOT NULL;
GO

CREATE INDEX [IX_IngestionJobs_Status_NextAttemptAt_LeaseExpiresAt] ON [IngestionJobs] ([Status], [NextAttemptAt], [LeaseExpiresAt]);
GO

ALTER TABLE [IngestionJobs] ADD CONSTRAINT [CK_IngestionJobs_DetailedCounts] CHECK ([RecordsUnchanged] >= 0 AND [RecordsFailed] >= 0 AND [RecordsAccepted] + [RecordsRejected] + [RecordsUnchanged] + [RecordsFailed] <= [RecordsFetched]);
GO

CREATE INDEX [IX_ExplorationEvents_SourceRevisionId_SourceId] ON [ExplorationEvents] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_ArticleVersions_CategorySlug_LanguageCode] ON [ArticleVersions] ([CategorySlug], [LanguageCode]);
GO

CREATE INDEX [IX_ArticleVersions_ReviewedById] ON [ArticleVersions] ([ReviewedById]);
GO

ALTER TABLE [ArticleVersions] ADD CONSTRAINT [CK_ArticleVersions_ReviewState] CHECK ([ReviewState] IN (N'Draft', N'InReview', N'Approved', N'ChangesRequested', N'Rejected', N'Superseded'));
GO

CREATE INDEX [IX_ArticleSources_SourceRevisionId_SourceId] ON [ArticleSources] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_Articles_CurrentDraftVersionId_Id] ON [Articles] ([CurrentDraftVersionId], [Id]);
GO

CREATE INDEX [IX_Articles_PublishedVersionId_Id] ON [Articles] ([PublishedVersionId], [Id]);
GO

CREATE INDEX [IX_ArticleExplorationEvents_ExplorationEventId] ON [ArticleExplorationEvents] ([ExplorationEventId]);
GO

CREATE INDEX [IX_ArticleLocations_LocationId] ON [ArticleLocations] ([LocationId]);
GO

CREATE INDEX [IX_ArticleMapLayers_MapLayerId] ON [ArticleMapLayers] ([MapLayerId]);
GO

CREATE INDEX [IX_ArticleMeasurements_MeasurementId] ON [ArticleMeasurements] ([MeasurementId]);
GO

CREATE INDEX [IX_ArticleMissions_MissionId] ON [ArticleMissions] ([MissionId]);
GO

CREATE INDEX [IX_ArticleVersionSources_SourceRevisionId_SourceId] ON [ArticleVersionSources] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_IngestionItems_ArchivedAt] ON [IngestionItems] ([ArchivedAt]);
GO

CREATE UNIQUE INDEX [IX_IngestionItems_JobId_ItemKey] ON [IngestionItems] ([JobId], [ItemKey]);
GO

CREATE INDEX [IX_IngestionItems_SourceId] ON [IngestionItems] ([SourceId]);
GO

CREATE INDEX [IX_IngestionItems_SourceRevisionId_SourceId] ON [IngestionItems] ([SourceRevisionId], [SourceId]);
GO

CREATE INDEX [IX_PublicationResources_Kind_Date] ON [PublicationResources] ([Kind], [Date]);
GO

CREATE INDEX [IX_PublicationResources_Kind_ResourceId] ON [PublicationResources] ([Kind], [ResourceId]);
GO

CREATE INDEX [IX_SourceRevisions_ArchivedAt] ON [SourceRevisions] ([ArchivedAt]);
GO

CREATE INDEX [IX_SourceRevisions_IngestionJobId] ON [SourceRevisions] ([IngestionJobId]);
GO

CREATE UNIQUE INDEX [IX_SourceRevisions_SourceId_Fingerprint] ON [SourceRevisions] ([SourceId], [Fingerprint]);
GO

CREATE UNIQUE INDEX [IX_SourceRevisions_SourceId_RevisionNumber] ON [SourceRevisions] ([SourceId], [RevisionNumber]);
GO

CREATE INDEX [IX_SourceRevisions_VerifiedById] ON [SourceRevisions] ([VerifiedById]);
GO

ALTER TABLE [Articles] ADD CONSTRAINT [FK_Articles_ArticleVersions_CurrentDraftVersionId_Id] FOREIGN KEY ([CurrentDraftVersionId], [Id]) REFERENCES [ArticleVersions] ([Id], [ArticleId]);
GO

ALTER TABLE [Articles] ADD CONSTRAINT [FK_Articles_ArticleVersions_PublishedVersionId_Id] FOREIGN KEY ([PublishedVersionId], [Id]) REFERENCES [ArticleVersions] ([Id], [ArticleId]);
GO

ALTER TABLE [ArticleSources] ADD CONSTRAINT [FK_ArticleSources_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [ArticleVersions] ADD CONSTRAINT [FK_ArticleVersions_Users_ReviewedById] FOREIGN KEY ([ReviewedById]) REFERENCES [Users] ([Id]);
GO

ALTER TABLE [ExplorationEvents] ADD CONSTRAINT [FK_ExplorationEvents_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [MapLayers] ADD CONSTRAINT [FK_MapLayers_MarsLocations_LocationId] FOREIGN KEY ([LocationId]) REFERENCES [MarsLocations] ([Id]);
GO

ALTER TABLE [MapLayers] ADD CONSTRAINT [FK_MapLayers_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [MarsLocations] ADD CONSTRAINT [FK_MarsLocations_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [Measurements] ADD CONSTRAINT [FK_Measurements_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [MediaAssets] ADD CONSTRAINT [FK_MediaAssets_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [Missions] ADD CONSTRAINT [FK_Missions_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [ScientificFacts] ADD CONSTRAINT [FK_ScientificFacts_SourceRevisions_SourceRevisionId_SourceId] FOREIGN KEY ([SourceRevisionId], [SourceId]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

ALTER TABLE [ScientificSources] ADD CONSTRAINT [FK_ScientificSources_SourceRevisions_CurrentAcceptedRevisionId_Id] FOREIGN KEY ([CurrentAcceptedRevisionId], [Id]) REFERENCES [SourceRevisions] ([Id], [SourceId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261102134845_AddDiscoverMarsVersioning', N'8.0.33');
GO

COMMIT;
GO

