USE [PharmaceuticalCompany];
GO

IF OBJECT_ID('dbo.usp_SyncLatestRecalls', 'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_SyncLatestRecalls;
GO

CREATE PROCEDURE dbo.usp_SyncLatestRecalls
    @DrugName       NVARCHAR(200),
    @BatchNumber    NVARCHAR(100) = NULL,
    @Reason         NVARCHAR(MAX) = NULL,
    @RecallDate     DATETIME,
    @RecallLevel    NVARCHAR(50)  = NULL,
    @SourceUrl      NVARCHAR(500)= NULL,
    @CompanyId      INT          = NULL,
    @SideEffects    NVARCHAR(MAX)= NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @SourceUrl IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.DrugRecalls WHERE SourceUrl = @SourceUrl)
    BEGIN
        UPDATE dbo.DrugRecalls
        SET
            DrugName    = ISNULL(NULLIF(@DrugName,''), DrugName),
            BatchNumber = COALESCE(@BatchNumber, BatchNumber),
            Reason      = COALESCE(@Reason, Reason),
            RecallDate  = @RecallDate,
            RecallLevel = COALESCE(@RecallLevel, RecallLevel),
            CompanyId   = COALESCE(@CompanyId, CompanyId),
            SideEffects = COALESCE(@SideEffects, SideEffects)
        WHERE SourceUrl = @SourceUrl;
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM dbo.DrugRecalls
        WHERE DrugName = @DrugName
          AND ISNULL(BatchNumber,'') = ISNULL(@BatchNumber,'')
          AND RecallDate = @RecallDate
    )
    BEGIN
        UPDATE dbo.DrugRecalls
        SET
            Reason      = COALESCE(@Reason, Reason),
            RecallLevel = COALESCE(@RecallLevel, RecallLevel),
            SourceUrl   = COALESCE(@SourceUrl, SourceUrl),
            CompanyId   = COALESCE(@CompanyId, CompanyId),
            SideEffects = COALESCE(@SideEffects, SideEffects)
        WHERE DrugName = @DrugName
          AND ISNULL(BatchNumber,'') = ISNULL(@BatchNumber,'')
          AND RecallDate = @RecallDate;
        RETURN;
    END

    INSERT INTO dbo.DrugRecalls
    (
        DrugName, SideEffects, BatchNumber, Reason, RecallDate, RecallLevel,
        SourceUrl, CompanyId, IsApproved, ApprovalStatus, SubmittedAt
    )
    VALUES
    (
        @DrugName, @SideEffects, @BatchNumber, @Reason, @RecallDate, @RecallLevel,
        @SourceUrl, @CompanyId, 1, 'Approved', GETDATE()
    );
END
GO
