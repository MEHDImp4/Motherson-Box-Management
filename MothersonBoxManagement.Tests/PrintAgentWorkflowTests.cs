using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Printing;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Tests;

public class PrintAgentWorkflowTests
{
    [Fact]
    public async Task PairingCode_IsSingleUse_AndStoresOnlyTokenHash()
    {
        await using var db = CreateDb();
        var workstation = NewWorkstation("P3-01");
        db.PrinterConfigurations.Add(workstation);
        await db.SaveChangesAsync();
        var service = new PrintAgentService(db, TimeProvider.System);

        var code = await service.CreatePairingCodeAsync(workstation.Id);
        var result = await service.PairAsync(code, "PC-P3-01", "1.0.0");

        Assert.NotEmpty(result.Token);
        Assert.NotEqual(result.Token, workstation.AgentTokenHash);
        Assert.Equal(PrintAgentSecurity.Hash(result.Token), workstation.AgentTokenHash);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PairAsync(code, "PC-P3-01", "1.0.0"));
    }

    [Fact]
    public async Task ExpiredPairingCode_IsRejected()
    {
        var now = new DateTimeOffset(2026, 7, 13, 10, 0, 0, TimeSpan.Zero);
        var clock = new MutableTimeProvider(now);
        await using var db = CreateDb();
        var workstation = NewWorkstation("P3-02");
        db.PrinterConfigurations.Add(workstation);
        await db.SaveChangesAsync();
        var service = new PrintAgentService(db, clock);
        var code = await service.CreatePairingCodeAsync(workstation.Id);
        clock.Advance(TimeSpan.FromMinutes(11));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.PairAsync(code, "PC-P3-02", "1.0.0"));
    }

    [Fact]
    public async Task Claim_IsIsolatedByWorkstation_AndLeasePreventsDoubleClaim()
    {
        await using var db = CreateDb();
        var user = NewUser();
        var box = NewBox(user);
        var first = NewWorkstation("P3-03");
        var second = NewWorkstation("P3-04");
        db.AddRange(user, box, first, second);
        await db.SaveChangesAsync();
        var service = new PrintAgentService(db, TimeProvider.System);
        await service.QueueAsync(box.Id, user.Id, first.Id);

        var wrongStation = await service.ClaimNextAsync(second.Id);
        var claimed = await service.ClaimNextAsync(first.Id);
        var duplicate = await service.ClaimNextAsync(first.Id);

        Assert.Null(wrongStation);
        Assert.NotNull(claimed);
        Assert.Equal(PrintJobStatuses.Claimed, claimed!.Status);
        Assert.Null(duplicate);
    }

    [Fact]
    public async Task UnavailablePrinter_LeavesQueuedLabelPendingUntilItIsReportedAgain()
    {
        await using var db = CreateDb();
        var user = NewUser();
        var box = NewBox(user);
        var workstation = NewWorkstation("P3-OFFLINE");
        workstation.AvailablePrintersJson = "[\"Microsoft Print to PDF\"]";
        db.AddRange(user, box, workstation);
        await db.SaveChangesAsync();
        var service = new PrintAgentService(db, TimeProvider.System);
        var job = await service.QueueAsync(box.Id, user.Id, workstation.Id);

        var claim = await service.ClaimNextAsync(workstation.Id);
        var stored = await db.BoxPrintJobs.FindAsync(job.Id);

        Assert.Null(claim);
        Assert.Equal(PrintJobStatuses.Pending, stored!.Status);
    }

    [Fact]
    public async Task TransientFailure_RetriesThreeTimes_ThenFailsPermanently()
    {
        await using var db = CreateDb();
        var user = NewUser();
        var box = NewBox(user);
        var workstation = NewWorkstation("P3-05");
        db.AddRange(user, box, workstation);
        await db.SaveChangesAsync();
        var service = new PrintAgentService(db, TimeProvider.System);
        var job = await service.QueueAsync(box.Id, user.Id, workstation.Id);

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            var claim = await service.ClaimNextAsync(workstation.Id, ignoreRetryDelay: true);
            Assert.NotNull(claim);
            await service.FailAsync(workstation.Id, job.Id, claim!.LeaseToken, "PRINTER_UNAVAILABLE", transient: true);
        }

        var stored = await db.BoxPrintJobs.FindAsync(job.Id);
        Assert.Equal(3, stored!.RetryCount);
        Assert.Equal(PrintJobStatuses.Failed, stored.Status);
        Assert.DoesNotContain("Exception", stored.ErrorMessage ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task TemplateCreation_QueuesLabelForConfiguredWorkstation()
    {
        await using var db = CreateDb();
        var user = NewUser();
        var workstation = NewWorkstation("P3-AUTO");
        var template = new BoxTemplate
        {
            Name = "Automatic print template",
            Type = BoxType.Cardboard,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 5,
            IsActive = true,
            CreatedBy = user,
            CreatedAt = DateTime.UtcNow
        };
        db.AddRange(user, workstation, template);
        await db.SaveChangesAsync();
        var printService = new PrintAgentService(db, TimeProvider.System);
        var templateService = new BoxTemplateService(db, new FixedBarcodeService(), printService);

        var box = await templateService.CreateBoxFromTemplateAsync(template.Id, user.Id, workstation.Code);

        var job = await db.BoxPrintJobs.SingleAsync(candidate => candidate.BoxId == box.Id);
        Assert.Equal(workstation.Id, job.WorkstationId);
        Assert.Equal(PrintJobStatuses.Pending, job.Status);
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static PrinterConfiguration NewWorkstation(string code) => new()
    {
        Code = code,
        PcName = code,
        PrinterName = "ZDesigner ZT411",
        PrintMode = PrintModes.Zpl,
        AvailablePrintersJson = "[\"ZDesigner ZT411\"]",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static User NewUser() => new()
    {
        Matricule = $"U{Guid.NewGuid():N}"[..10],
        FullName = "Print Agent Test",
        PasswordHash = "not-used",
        Role = "Administrator",
        SecurityStamp = Guid.NewGuid().ToString("N"),
        IsActive = true
    };

    private static Box NewBox(User user) => new()
    {
        BoxNumber = $"BOX-{Guid.NewGuid():N}"[..20],
        BarcodeValue = $"BOX-{Guid.NewGuid():N}"[..20],
        Type = BoxType.Cardboard,
        Height = 10,
        Width = 10,
        Depth = 10,
        ExpectedQuantity = 1,
        Status = BoxStatus.Open,
        CreatedBy = user,
        CreatedAt = DateTime.UtcNow
    };

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;
        public override DateTimeOffset GetUtcNow() => _utcNow;
        public void Advance(TimeSpan duration) => _utcNow += duration;
    }

    private sealed class FixedBarcodeService : IBarcodeService
    {
        public Task<string> GenerateUniqueBoxBarcodeAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult($"BOX-{Guid.NewGuid():N}"[..20]);
        public bool IsBoxBarcode(string barcode) => true;
        public Task<bool> IsBoxBarcodeAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public bool IsPackageBarcode(string barcode) => true;
        public Task<bool> IsPackageBarcodeAsync(string barcode, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public void ValidateBarcodeFormat(string barcode, bool expectBox) { }
        public Task ValidateBarcodeFormatAsync(string barcode, bool expectBox, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
