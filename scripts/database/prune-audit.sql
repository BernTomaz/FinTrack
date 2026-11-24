-- @Cutoff (datetime2) and @Apply (bit) are supplied by the caller.
SET NOCOUNT ON;
SELECT @Cutoff AS CutoffUtc;
SELECT COUNT_BIG(*) AS EligibleBefore
FROM [$(DatabaseName)].[dbo].[AuditEvents] WHERE OccurredAtUtc < @Cutoff;
IF @Apply = 1
BEGIN
    DELETE TOP (10000) FROM [$(DatabaseName)].[dbo].[AuditEvents]
    WHERE OccurredAtUtc < @Cutoff;
    SELECT @@ROWCOUNT AS Deleted;
    SELECT COUNT_BIG(*) AS EligibleRemaining
    FROM [$(DatabaseName)].[dbo].[AuditEvents] WHERE OccurredAtUtc < @Cutoff;
END;
