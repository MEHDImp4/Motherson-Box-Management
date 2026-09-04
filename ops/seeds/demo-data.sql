/* ============================================================================
   Motherson Box Management - Presentation demo data
   ----------------------------------------------------------------------------
   Idempotent seed script. Safe to run multiple times (guards on unique keys).
   Insert this into the running SQL Server via:

     docker cp ops/seeds/demo-data.sql motherson-sqlserver:/tmp/demo-data.sql
     docker exec motherson-sqlserver /opt/mssql-tools18/bin/sqlcmd \
       -S localhost -U sa -P '<MSSQL_SA_PASSWORD>' -C -d MothersonBoxDb \
       -b -i /tmp/demo-data.sql

   Depends on the Development seed users already present:
       OP001 (Operator, id 1), SP001 (Supervisor, id 2), AD001 (Administrator, id 3)
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @op INT = (SELECT Id FROM Users WHERE Matricule = 'OP001');
    DECLARE @sp INT = (SELECT Id FROM Users WHERE Matricule = 'SP001');
    DECLARE @ad INT = (SELECT Id FROM Users WHERE Matricule = 'AD001');

    /* ============================ BOX TEMPLATES ============================ */
    IF NOT EXISTS (SELECT 1 FROM BoxTemplates WHERE Name = 'Carton Standard 40x30x20')
        INSERT INTO BoxTemplates
            (Name, Description, Type, Height, Width, Depth, ExpectedQuantity, PackagePrefixPattern, IsActive, CreatedByUserId, CreatedAt)
        VALUES
            ('Carton Standard 40x30x20', 'Carton double cannelure pour faisceaux moteur.', 0, 40, 30, 20, 12, NULL, 1, @ad, '2026-07-20T09:00:00');

    IF NOT EXISTS (SELECT 1 FROM BoxTemplates WHERE Name = 'Caisse Bois 80x60x50')
        INSERT INTO BoxTemplates
            (Name, Description, Type, Height, Width, Depth, ExpectedQuantity, PackagePrefixPattern, IsActive, CreatedByUserId, CreatedAt)
        VALUES
            ('Caisse Bois 80x60x50', 'Caisse en bois renforcee pour pieces lourdes.', 1, 80, 60, 50, 4, NULL, 1, @sp, '2026-07-20T09:05:00');

    IF NOT EXISTS (SELECT 1 FROM BoxTemplates WHERE Name = 'Bac Plastique 60x40x35')
        INSERT INTO BoxTemplates
            (Name, Description, Type, Height, Width, Depth, ExpectedQuantity, PackagePrefixPattern, IsActive, CreatedByUserId, CreatedAt)
        VALUES
            ('Bac Plastique 60x40x35', 'Bac plastique reutilisable pour composants.', 2, 60, 40, 35, 8, NULL, 1, @ad, '2026-07-21T10:00:00');

    /* ========================================================================
       BOX 1 - Completed automatically (cardboard, 12/12 packages, STATION-01)
       ======================================================================== */
    IF NOT EXISTS (SELECT 1 FROM Boxes WHERE BoxNumber = 'BOX-20260801-482913')
    BEGIN
        INSERT INTO Boxes
            (BoxNumber, BarcodeValue, Type, Height, Width, Depth, ExpectedQuantity, CurrentQuantity, Status,
             CreatedByUserId, LastModifiedByUserId, CompletedByUserId, CreatedAt, ModifiedAt, CompletedAt, CompletionMode)
        VALUES
            ('BOX-20260801-482913', 'BOX-20260801-482913', 0, 40, 30, 20, 12, 12, 1,
             @op, @op, @op, '2026-08-01T07:40:00', '2026-08-01T08:25:00', '2026-08-01T08:25:00', 'Auto');

        DECLARE @box1 INT = SCOPE_IDENTITY();

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box1, 'BoxCreated', @op, '2026-08-01T07:40:00', 'STATION-01', 'New box created.');
        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box1, 'BoxOpened', @op, '2026-08-01T07:40:05', 'STATION-01', 'Box opened and QR code generated.');

        DECLARE @i1 INT = 1;
        WHILE @i1 <= 12
        BEGIN
            DECLARE @bc1 VARCHAR(40) = CONCAT('CBL/2026-08-01/A', RIGHT('000' + CAST(@i1 AS VARCHAR(3)), 3));
            INSERT INTO BoxPackages (BoxId, PackageBarcode, ScannedByUserId, ScannedAt, WorkstationName, IsBlocked, IsRemoved)
            VALUES (@box1, @bc1, @op, DATEADD(MINUTE, @i1 * 3, '2026-08-01T07:40:00'), 'STATION-01', 0, 0);
            INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Description)
            VALUES (@box1, @bc1, 'PackageScanned', @op, DATEADD(MINUTE, @i1 * 3, '2026-08-01T07:40:00'), 'STATION-01', 'Package scanned and assigned to a box.');
            SET @i1 = @i1 + 1;
        END;

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, PreviousValue, NewValue, Description)
        VALUES (@box1, 'BoxCompletedAuto', @op, '2026-08-01T08:25:00', 'STATION-01', 'Open', 'Completed', 'Box completed automatically.');

        INSERT INTO BoxPrintJobs (BoxId, PrinterName, PrinterUncPath, PayloadType, RequestedByUserId, RetryCount, RequestedAt, PrintedAt, Status)
        VALUES (@box1, 'Zebra ZT410 (STATION-01)', '\\\\STATION-01\\Zebra', 'Zpl', @op, 0, '2026-08-01T07:40:10', '2026-08-01T07:40:20', 'Printed');
    END;

    /* ========================================================================
       BOX 2 - Open, being filled (plastic, 3/8 packages, STATION-01)
       ======================================================================== */
    IF NOT EXISTS (SELECT 1 FROM Boxes WHERE BoxNumber = 'BOX-20260803-115240')
    BEGIN
        INSERT INTO Boxes
            (BoxNumber, BarcodeValue, Type, Height, Width, Depth, ExpectedQuantity, CurrentQuantity, Status,
             CreatedByUserId, LastModifiedByUserId, CreatedAt)
        VALUES
            ('BOX-20260803-115240', 'BOX-20260803-115240', 2, 60, 40, 35, 8, 3, 0,
             @op, @op, '2026-08-03T11:52:00');

        DECLARE @box2 INT = SCOPE_IDENTITY();

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box2, 'BoxCreated', @op, '2026-08-03T11:52:00', 'STATION-01', 'New box created.');
        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box2, 'BoxOpened', @op, '2026-08-03T11:52:08', 'STATION-01', 'Box opened and QR code generated.');

        DECLARE @i2 INT = 1;
        WHILE @i2 <= 3
        BEGIN
            DECLARE @bc2 VARCHAR(40) = CONCAT('PLT/2026-08-03/B', RIGHT('000' + CAST(@i2 AS VARCHAR(3)), 3));
            INSERT INTO BoxPackages (BoxId, PackageBarcode, ScannedByUserId, ScannedAt, WorkstationName, IsBlocked, IsRemoved)
            VALUES (@box2, @bc2, @op, DATEADD(MINUTE, @i2 * 4, '2026-08-03T11:52:00'), 'STATION-01', 0, 0);
            INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Description)
            VALUES (@box2, @bc2, 'PackageScanned', @op, DATEADD(MINUTE, @i2 * 4, '2026-08-03T11:52:00'), 'STATION-01', 'Package scanned and assigned to a box.');
            SET @i2 = @i2 + 1;
        END;

        INSERT INTO BoxPrintJobs (BoxId, PrinterName, PrinterUncPath, PayloadType, RequestedByUserId, RetryCount, RequestedAt, PrintedAt, Status)
        VALUES (@box2, 'Zebra ZT410 (STATION-01)', '\\\\STATION-01\\Zebra', 'Zpl', @op, 0, '2026-08-03T11:52:10', '2026-08-03T11:52:25', 'Printed');
    END;

    /* ========================================================================
       BOX 3 - Open, empty (cardboard, 0/12, STATION-02) - ready for live demo
       ======================================================================== */
    IF NOT EXISTS (SELECT 1 FROM Boxes WHERE BoxNumber = 'BOX-20260804-072318')
    BEGIN
        INSERT INTO Boxes
            (BoxNumber, BarcodeValue, Type, Height, Width, Depth, ExpectedQuantity, CurrentQuantity, Status,
             CreatedByUserId, LastModifiedByUserId, CreatedAt)
        VALUES
            ('BOX-20260804-072318', 'BOX-20260804-072318', 0, 40, 30, 20, 12, 0, 0,
             @op, @op, '2026-08-04T07:23:00');

        DECLARE @box3 INT = SCOPE_IDENTITY();

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box3, 'BoxCreated', @op, '2026-08-04T07:23:00', 'STATION-02', 'New box created.');
        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box3, 'BoxOpened', @op, '2026-08-04T07:23:06', 'STATION-02', 'Box opened and QR code generated.');
    END;


    /* ========================================================================
       BOX 4 - Completed with exception (wood, 3/4, one package removed, STATION-03)
       ======================================================================== */
    IF NOT EXISTS (SELECT 1 FROM Boxes WHERE BoxNumber = 'BOX-20260731-604115')
    BEGIN
        INSERT INTO Boxes
            (BoxNumber, BarcodeValue, Type, Height, Width, Depth, ExpectedQuantity, CurrentQuantity, Status,
             CreatedByUserId, LastModifiedByUserId, CompletedByUserId, CreatedAt, ModifiedAt, CompletedAt,
             CompletionMode, ExceptionReason)
        VALUES
            ('BOX-20260731-604115', 'BOX-20260731-604115', 1, 80, 60, 50, 4, 3, 2,
             @op, @sp, @sp, '2026-07-31T06:15:00', '2026-07-31T09:40:00', '2026-07-31T09:40:00',
             'WithException', 'Boite fermee par supervision : 1 colis retire, quantite attendue non atteinte.');

        DECLARE @box4 INT = SCOPE_IDENTITY();

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box4, 'BoxCreated', @op, '2026-07-31T06:15:00', 'STATION-03', 'New box created.');
        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box4, 'BoxOpened', @op, '2026-07-31T06:15:04', 'STATION-03', 'Box opened and QR code generated.');

        DECLARE @i4 INT = 1;
        WHILE @i4 <= 4
        BEGIN
            DECLARE @bc4 VARCHAR(40) = CONCAT('HW/2026-07-31/C', RIGHT('000' + CAST(@i4 AS VARCHAR(3)), 3));
            INSERT INTO BoxPackages (BoxId, PackageBarcode, ScannedByUserId, ScannedAt, WorkstationName, IsBlocked, IsRemoved)
            VALUES (@box4, @bc4, @op, DATEADD(MINUTE, @i4 * 5, '2026-07-31T06:15:00'), 'STATION-03', 0, 0);
            INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Description)
            VALUES (@box4, @bc4, 'PackageScanned', @op, DATEADD(MINUTE, @i4 * 5, '2026-07-31T06:15:00'), 'STATION-03', 'Package scanned and assigned to a box.');
            SET @i4 = @i4 + 1;
        END;

        /* Supervisor removes the 4th package (quality defect) */
        UPDATE BoxPackages
           SET IsRemoved = 1, RemovedAt = '2026-07-31T09:20:00', RemovedByUserId = @sp,
               RemovalReason = 'Colis endommage constate au controle qualite.'
         WHERE BoxId = @box4 AND PackageBarcode = 'HW/2026-07-31/C004';

        INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Reason, Description)
        VALUES (@box4, 'HW/2026-07-31/C004', 'PackageRemoved', @sp, '2026-07-31T09:20:00', 'STATION-03',
                'Colis endommage constate au controle qualite.', 'Package removed from the box.');

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, PreviousValue, NewValue, Reason, Description)
        VALUES (@box4, 'BoxCompletedWithException', @sp, '2026-07-31T09:40:00', 'STATION-03', 'Open', 'CompletedWithException',
                'Boite fermee par supervision avec exception.', 'Box closed with an exception.');

        INSERT INTO BoxPrintJobs (BoxId, PrinterName, PrinterUncPath, PayloadType, RequestedByUserId, RetryCount, RequestedAt, PrintedAt, Status)
        VALUES (@box4, 'Zebra ZT410 (STATION-03)', '\\\\STATION-03\\Zebra', 'Zpl', @op, 0, '2026-07-31T09:40:10', '2026-07-31T09:40:22', 'Printed');
    END;


    /* ========================================================================
       BOX 5 - Cancelled (cardboard, packages disassociated, STATION-01)
       ======================================================================== */
    IF NOT EXISTS (SELECT 1 FROM Boxes WHERE BoxNumber = 'BOX-20260730-871204')
    BEGIN
        INSERT INTO Boxes
            (BoxNumber, BarcodeValue, Type, Height, Width, Depth, ExpectedQuantity, CurrentQuantity, Status,
             CreatedByUserId, LastModifiedByUserId, CreatedAt, ModifiedAt, CompletionMode)
        VALUES
            ('BOX-20260730-871204', 'BOX-20260730-871204', 0, 40, 30, 20, 10, 0, 3,
             @op, @sp, '2026-07-30T14:05:00', '2026-07-30T15:30:00', 'Manual');

        DECLARE @box5 INT = SCOPE_IDENTITY();

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box5, 'BoxCreated', @op, '2026-07-30T14:05:00', 'STATION-01', 'New box created.');
        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box5, 'BoxOpened', @op, '2026-07-30T14:05:05', 'STATION-01', 'Box opened and QR code generated.');

        DECLARE @i5 INT = 1;
        WHILE @i5 <= 2
        BEGIN
            DECLARE @bc5 VARCHAR(40) = CONCAT('CBL/2026-07-30/D', RIGHT('000' + CAST(@i5 AS VARCHAR(3)), 3));
            INSERT INTO BoxPackages (BoxId, PackageBarcode, ScannedByUserId, ScannedAt, WorkstationName, IsBlocked, IsRemoved)
            VALUES (@box5, @bc5, @op, DATEADD(MINUTE, @i5 * 4, '2026-07-30T14:05:00'), 'STATION-01', 0, 0);
            INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Description)
            VALUES (@box5, @bc5, 'PackageScanned', @op, DATEADD(MINUTE, @i5 * 4, '2026-07-30T14:05:00'), 'STATION-01', 'Package scanned and assigned to a box.');
            SET @i5 = @i5 + 1;
        END;

        /* Supervisor cancels the box */
        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, PreviousValue, NewValue, Reason, Description)
        VALUES (@box5, 'BoxCancelled', @sp, '2026-07-30T15:30:00', 'STATION-01', 'Open', 'Cancelled',
                'Erreur de reference produit sur l''ensemble de la boite.', 'Box cancelled.');

        /* Packages are disassociated from the cancelled box */
        UPDATE BoxPackages
           SET IsRemoved = 1, RemovedAt = '2026-07-30T15:30:05', RemovedByUserId = @sp,
               RemovalReason = 'Colis dissocies suite a l''annulation de la boite.'
         WHERE BoxId = @box5;

        INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box5, 'CBL/2026-07-30/D001', 'PackageDisassociated', @sp, '2026-07-30T15:30:05', 'STATION-01', 'Package disassociated from a cancelled box.');
        INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box5, 'CBL/2026-07-30/D002', 'PackageDisassociated', @sp, '2026-07-30T15:30:05', 'STATION-01', 'Package disassociated from a cancelled box.');
    END;


    /* ========================================================================
       BOX 6 - Blocked / quarantine (plastic, 1/8, STATION-04)
       ======================================================================== */
    IF NOT EXISTS (SELECT 1 FROM Boxes WHERE BoxNumber = 'BOX-20260802-339015')
    BEGIN
        INSERT INTO Boxes
            (BoxNumber, BarcodeValue, Type, Height, Width, Depth, ExpectedQuantity, CurrentQuantity, Status,
             CreatedByUserId, LastModifiedByUserId, BlockedByUserId, CreatedAt, ModifiedAt, BlockedAt, BlockReason)
        VALUES
            ('BOX-20260802-339015', 'BOX-20260802-339015', 2, 60, 40, 35, 8, 1, 5,
             @op, @sp, @sp, '2026-08-02T10:10:00', '2026-08-02T10:45:00', '2026-08-02T10:45:00',
             'Mise en quarantaine en attendant le resultat du controle qualite.');

        DECLARE @box6 INT = SCOPE_IDENTITY();

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box6, 'BoxCreated', @op, '2026-08-02T10:10:00', 'STATION-04', 'New box created.');
        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box6, 'BoxOpened', @op, '2026-08-02T10:10:06', 'STATION-04', 'Box opened and QR code generated.');

        INSERT INTO BoxPackages (BoxId, PackageBarcode, ScannedByUserId, ScannedAt, WorkstationName, IsBlocked, IsRemoved)
        VALUES (@box6, 'PLT/2026-08-02/E001', @op, '2026-08-02T10:11:00', 'STATION-04', 0, 0);

        INSERT INTO BoxAuditLogs (BoxId, PackageBarcode, ActionType, UserId, Timestamp, WorkstationName, Description)
        VALUES (@box6, 'PLT/2026-08-02/E001', 'PackageScanned', @op, '2026-08-02T10:11:00', 'STATION-04', 'Package scanned and assigned to a box.');

        INSERT INTO BoxAuditLogs (BoxId, ActionType, UserId, Timestamp, WorkstationName, PreviousValue, NewValue, Reason, Description)
        VALUES (@box6, 'BoxBlocked', @sp, '2026-08-02T10:45:00', 'STATION-04', 'Open', 'Blocked',
                'Mise en quarantaine en attendant le resultat du controle qualite.', 'Box blocked.');
    END;

    COMMIT TRANSACTION;
    PRINT 'Demo data inserted successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
    THROW;
END CATCH;

