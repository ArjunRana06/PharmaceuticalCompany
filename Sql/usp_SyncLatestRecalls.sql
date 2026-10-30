USE [PharmaceuticalCompany];
GO

IF OBJECT_ID('dbo.usp_SyncLatestRecalls', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SyncLatestRecalls;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE PROCEDURE dbo.usp_SyncLatestRecalls
    @DrugName       NVARCHAR(200),
    @BatchNumber    NVARCHAR(100) = NULL,
    @Reason         NVARCHAR(MAX) = NULL,
    @RecallDate     DATETIME,
    @RecallLevel    NVARCHAR(50)  = NULL,
    @SourceUrl      NVARCHAR(500) = NULL,
    @SourceName     NVARCHAR(50)  = NULL,
    @ExternalId     NVARCHAR(100) = NULL,
    @CompanyName    NVARCHAR(200) = NULL,
    @SideEffects    NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CompanyId INT = NULL;

    IF @CompanyName IS NOT NULL AND LEN(LTRIM(RTRIM(@CompanyName))) >= 4
    BEGIN
        SET @CompanyName = LTRIM(RTRIM(@CompanyName));

        SELECT TOP 1 @CompanyId = Id
        FROM dbo.Companies
        WHERE Name = @CompanyName
           OR Name LIKE '%' + @CompanyName + '%'
           OR @CompanyName LIKE '%' + Name + '%'
        ORDER BY CASE WHEN Name = @CompanyName THEN 0 ELSE 1 END, LEN(Name);
    END

    IF @ExternalId IS NOT NULL AND EXISTS (
        SELECT 1
        FROM dbo.DrugRecalls
        WHERE ISNULL(SourceName, '') = ISNULL(@SourceName, '')
          AND ExternalId = @ExternalId
    )
    BEGIN
        UPDATE dbo.DrugRecalls
        SET
            DrugName    = ISNULL(NULLIF(@DrugName, ''), DrugName),
            BatchNumber = COALESCE(@BatchNumber, BatchNumber),
            Reason      = COALESCE(@Reason, Reason),
            RecallDate  = @RecallDate,
            RecallLevel = COALESCE(@RecallLevel, RecallLevel),
            SourceUrl   = COALESCE(@SourceUrl, SourceUrl),
            CompanyId   = COALESCE(@CompanyId, CompanyId),
            SideEffects = COALESCE(@SideEffects, SideEffects)
        WHERE ISNULL(SourceName, '') = ISNULL(@SourceName, '')
          AND ExternalId = @ExternalId;
        RETURN;
    END

    IF @SourceUrl IS NOT NULL AND EXISTS (
        SELECT 1 FROM dbo.DrugRecalls WHERE SourceUrl = @SourceUrl
    )
    BEGIN
        UPDATE dbo.DrugRecalls
        SET
            DrugName    = ISNULL(NULLIF(@DrugName, ''), DrugName),
            BatchNumber = COALESCE(@BatchNumber, BatchNumber),
            Reason      = COALESCE(@Reason, Reason),
            RecallDate  = @RecallDate,
            RecallLevel = COALESCE(@RecallLevel, RecallLevel),
            SourceName  = COALESCE(@SourceName, SourceName),
            ExternalId  = COALESCE(@ExternalId, ExternalId),
            CompanyId   = COALESCE(@CompanyId, CompanyId),
            SideEffects = COALESCE(@SideEffects, SideEffects)
        WHERE SourceUrl = @SourceUrl;
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM dbo.DrugRecalls
        WHERE DrugName = @DrugName
          AND ISNULL(BatchNumber, '') = ISNULL(@BatchNumber, '')
          AND RecallDate = @RecallDate
    )
    BEGIN
        UPDATE dbo.DrugRecalls
        SET
            Reason      = COALESCE(@Reason, Reason),
            RecallLevel = COALESCE(@RecallLevel, RecallLevel),
            SourceUrl   = COALESCE(@SourceUrl, SourceUrl),
            SourceName  = COALESCE(@SourceName, SourceName),
            ExternalId  = COALESCE(@ExternalId, ExternalId),
            CompanyId   = COALESCE(@CompanyId, CompanyId),
            SideEffects = COALESCE(@SideEffects, SideEffects)
        WHERE DrugName = @DrugName
          AND ISNULL(BatchNumber, '') = ISNULL(@BatchNumber, '')
          AND RecallDate = @RecallDate;
        RETURN;
    END

    INSERT INTO dbo.DrugRecalls
    (
        DrugName, SideEffects, BatchNumber, Reason, RecallDate, RecallLevel,
        SourceUrl, SourceName, ExternalId, CompanyId, IsApproved, ApprovalStatus, SubmittedAt
    )
    VALUES
    (
        @DrugName, @SideEffects, @BatchNumber, @Reason, @RecallDate, @RecallLevel,
        @SourceUrl, @SourceName, @ExternalId, @CompanyId, 1, 'Approved', GETDATE()
    );
END
GO
