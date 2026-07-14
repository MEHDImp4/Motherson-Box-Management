:on error exit
DECLARE @BackupFile nvarchar(4000) = N'$(BackupFile)';
ALTER DATABASE [$(RestoreDatabaseName)] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$(RestoreDatabaseName)]
FROM DISK = @BackupFile
WITH REPLACE, RECOVERY, STATS = 10;
ALTER DATABASE [$(RestoreDatabaseName)] SET MULTI_USER;
