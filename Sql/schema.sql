SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    CREATE TABLE [Companies] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Province] nvarchar(max) NOT NULL,
        [District] nvarchar(max) NOT NULL,
        [City] nvarchar(max) NOT NULL,
        [Address] nvarchar(max) NOT NULL,
        [Phone] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Companies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    CREATE TABLE [DdaNotices] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [NoticeDate] datetime2 NOT NULL,
        [NoticeType] nvarchar(max) NULL,
        [SourceUrl] nvarchar(max) NULL,
        CONSTRAINT [PK_DdaNotices] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    CREATE TABLE [Users] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Password] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    CREATE TABLE [DrugRecalls] (
        [Id] int NOT NULL IDENTITY,
        [DrugName] nvarchar(max) NOT NULL,
        [BatchNumber] nvarchar(max) NULL,
        [Reason] nvarchar(max) NULL,
        [RecallDate] datetime2 NOT NULL,
        [RecallLevel] nvarchar(max) NULL,
        [SourceUrl] nvarchar(max) NULL,
        [CompanyId] int NOT NULL,
        CONSTRAINT [PK_DrugRecalls] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DrugRecalls_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    CREATE TABLE [Drugs] (
        [Id] int NOT NULL IDENTITY,
        [DrugName] nvarchar(max) NOT NULL,
        [GenericName] nvarchar(max) NULL,
        [DosageForm] nvarchar(max) NULL,
        [Strength] nvarchar(max) NULL,
        [RegistrationNumber] nvarchar(max) NULL,
        [RegistrationDate] datetime2 NULL,
        [CompanyId] int NOT NULL,
        CONSTRAINT [PK_Drugs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Drugs_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DrugRecalls_CompanyId] ON [DrugRecalls] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Drugs_CompanyId] ON [Drugs] ([CompanyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260929150615_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260929150615_InitialCreate', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022081539_AddPharmaDatasetSchema'
)
BEGIN
    ALTER TABLE [Companies] ADD [DocumentedRecallEvents] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022081539_AddPharmaDatasetSchema'
)
BEGIN
    ALTER TABLE [Companies] ADD [ExportCapability] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022081539_AddPharmaDatasetSchema'
)
BEGIN
    ALTER TABLE [Companies] ADD [InternationalRecallRecords] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022081539_AddPharmaDatasetSchema'
)
BEGIN
    ALTER TABLE [Companies] ADD [SourceUrl] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022081539_AddPharmaDatasetSchema'
)
BEGIN
    CREATE TABLE [DrugCategories] (
        [Id] int NOT NULL IDENTITY,
        [Category] nvarchar(max) NOT NULL,
        [SubCategory] nvarchar(max) NULL,
        [Definition] nvarchar(max) NOT NULL,
        [PrescriptionRequired] nvarchar(max) NULL,
        [WhoMaySell] nvarchar(max) NULL,
        [LabelWarning] nvarchar(max) NULL,
        [LegalReference] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_DrugCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022081539_AddPharmaDatasetSchema'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022081539_AddPharmaDatasetSchema', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022082607_MakeCompanyRegionOptional'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'Province');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Companies] ALTER COLUMN [Province] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022082607_MakeCompanyRegionOptional'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'District');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Companies] ALTER COLUMN [District] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022082607_MakeCompanyRegionOptional'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022082607_MakeCompanyRegionOptional', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022083600_MakeCompanyContactOptional'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'Phone');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Companies] ALTER COLUMN [Phone] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022083600_MakeCompanyContactOptional'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'City');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Companies] ALTER COLUMN [City] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022083600_MakeCompanyContactOptional'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Companies]') AND [c].[name] = N'Address');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Companies] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Companies] ALTER COLUMN [Address] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261022083600_MakeCompanyContactOptional'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261022083600_MakeCompanyContactOptional', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [Users] ADD [Role] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [ApprovalStatus] nvarchar(max) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [ApprovedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [ApprovedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [IsApproved] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [RejectionReason] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [SideEffects] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [SubmittedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [SubmittedByUserId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    CREATE INDEX [IX_DrugRecalls_ApprovedByUserId] ON [DrugRecalls] ([ApprovedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    CREATE INDEX [IX_DrugRecalls_SubmittedByUserId] ON [DrugRecalls] ([SubmittedByUserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD CONSTRAINT [FK_DrugRecalls_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD CONSTRAINT [FK_DrugRecalls_Users_SubmittedByUserId] FOREIGN KEY ([SubmittedByUserId]) REFERENCES [Users] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261024124352_AddApprovalAndSideEffectsToDrugRecallsAndRoleToUser', N'10.0.12');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    ALTER TABLE [DrugRecalls] DROP CONSTRAINT [FK_DrugRecalls_Companies_CompanyId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DrugRecalls]') AND [c].[name] = N'SourceUrl');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [DrugRecalls] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [DrugRecalls] ALTER COLUMN [SourceUrl] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DrugRecalls]') AND [c].[name] = N'CompanyId');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [DrugRecalls] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [DrugRecalls] ALTER COLUMN [CompanyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[DrugRecalls]') AND [c].[name] = N'ApprovalStatus');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [DrugRecalls] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [DrugRecalls] ALTER COLUMN [ApprovalStatus] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [ExternalId] nvarchar(100) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD [SourceName] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    CREATE TABLE [RecallSyncLogs] (
        [Id] int NOT NULL IDENTITY,
        [StartedAt] datetime2 NOT NULL,
        [CompletedAt] datetime2 NULL,
        [Status] nvarchar(20) NOT NULL,
        [ItemsFetched] int NOT NULL,
        [ItemsSaved] int NOT NULL,
        [Message] nvarchar(1000) NULL,
        CONSTRAINT [PK_RecallSyncLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    CREATE INDEX [IX_DrugRecalls_IsApproved_ApprovalStatus_RecallDate] ON [DrugRecalls] ([IsApproved], [ApprovalStatus], [RecallDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_DrugRecalls_SourceName_ExternalId] ON [DrugRecalls] ([SourceName], [ExternalId]) WHERE [ExternalId] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    ALTER TABLE [DrugRecalls] ADD CONSTRAINT [FK_DrugRecalls_Companies_CompanyId] FOREIGN KEY ([CompanyId]) REFERENCES [Companies] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261029133644_AddRecallSyncSourceTracking'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261029133644_AddRecallSyncSourceTracking', N'10.0.12');
END;

COMMIT;
GO
