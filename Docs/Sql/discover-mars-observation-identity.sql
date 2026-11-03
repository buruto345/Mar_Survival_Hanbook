BEGIN TRANSACTION;
GO

DROP INDEX [IX_Measurements_SourceRevisionId_ExternalObservationKey] ON [Measurements];
DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Measurements]') AND [c].[name] = N'ExternalObservationKey');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Measurements] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Measurements] ALTER COLUMN [ExternalObservationKey] nvarchar(450) COLLATE Latin1_General_100_BIN2 NULL;
CREATE UNIQUE INDEX [IX_Measurements_SourceRevisionId_ExternalObservationKey] ON [Measurements] ([SourceRevisionId], [ExternalObservationKey]) WHERE [SourceRevisionId] IS NOT NULL AND [ExternalObservationKey] IS NOT NULL;
GO

DROP INDEX [IX_IngestionItems_JobId_ItemKey] ON [IngestionItems];
DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IngestionItems]') AND [c].[name] = N'ItemKey');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [IngestionItems] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [IngestionItems] ALTER COLUMN [ItemKey] nvarchar(450) COLLATE Latin1_General_100_BIN2 NOT NULL;
CREATE UNIQUE INDEX [IX_IngestionItems_JobId_ItemKey] ON [IngestionItems] ([JobId], [ItemKey]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261103041917_PreserveDiscoveryObservationIdentity', N'8.0.33');
GO

COMMIT;
GO

