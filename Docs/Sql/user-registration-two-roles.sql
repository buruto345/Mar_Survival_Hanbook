BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    ALTER TABLE [Users] DROP CONSTRAINT [CK_Users_Role];
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    EXEC sp_rename N'[Users].[DisplayName]', N'FullName', N'COLUMN';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    ALTER TABLE [Users] ADD [NormalizedUsername] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    ALTER TABLE [Users] ADD [Username] nvarchar(50) NOT NULL DEFAULT N'';
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    UPDATE [Users]
    SET [Username] = N'legacy_' + LOWER(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N'')),
        [NormalizedUsername] = N'LEGACY_' + UPPER(REPLACE(CONVERT(nvarchar(36), [Id]), N'-', N'')),
        [Role] = CASE WHEN [Role] = N'RegisteredReader' THEN N'Viewer' ELSE N'Admin' END;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Users_NormalizedUsername] ON [Users] ([NormalizedUsername]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    EXEC(N'ALTER TABLE [Users] ADD CONSTRAINT [CK_Users_Role] CHECK ([Role] IN (N''Viewer'', N''Admin''))');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261104010741_AddUserRegistrationAndTwoRoles'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261104010741_AddUserRegistrationAndTwoRoles', N'8.0.33');
END;
GO

COMMIT;
GO

