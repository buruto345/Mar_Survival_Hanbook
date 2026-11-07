BEGIN TRANSACTION;
GO

UPDATE ScientificSources SET Provider = N'ManualLegacy',
    IdentityKey = N'legacy:' + LOWER(CONVERT(nvarchar(36), Id))
WHERE IdentityKey IS NULL OR IdentityKey = N'';
INSERT INTO AuditLogs (Id, CreatedAt, Action, EntityType, EntityId, OccurredAt, ChangeSummary, MetadataJson)
SELECT NEWID(), SYSDATETIMEOFFSET(), N'Unpublished', N'Article', Id, SYSDATETIMEOFFSET(),
    N'Legacy publication requires reconstruction and scientific review; original content and versions retained.',
    (SELECT a.Status AS previousStatus, a.PublishedAt AS publishedAt,
        a.PublishedById AS publishedById, a.ReviewedAt AS reviewedAt,
        a.ReviewedById AS reviewedById FOR JSON PATH, WITHOUT_ARRAY_WRAPPER)
FROM Articles a WHERE Status = N'Published' AND PublishedVersionId IS NULL;
UPDATE Articles SET Status = N'Draft', RequiresReview = 1
WHERE Status = N'Published' AND PublishedVersionId IS NULL;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ScientificSources]') AND [c].[name] = N'IdentityKey');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [ScientificSources] DROP CONSTRAINT [' + @var0 + '];');
UPDATE [ScientificSources] SET [IdentityKey] = N'' WHERE [IdentityKey] IS NULL;
ALTER TABLE [ScientificSources] ALTER COLUMN [IdentityKey] nvarchar(450) COLLATE Latin1_General_100_BIN2 NOT NULL;
ALTER TABLE [ScientificSources] ADD DEFAULT N'' FOR [IdentityKey];
GO

CREATE UNIQUE INDEX [IX_ScientificSources_Provider_IdentityKey] ON [ScientificSources] ([Provider], [IdentityKey]);
GO

ALTER TABLE [Articles] ADD CONSTRAINT [CK_Articles_PublicationPointer] CHECK ([Status] <> N'Published' OR [PublishedVersionId] IS NOT NULL);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261102135143_EnforceDiscoverMarsIntegrity', N'8.0.33');
GO

COMMIT;
GO

