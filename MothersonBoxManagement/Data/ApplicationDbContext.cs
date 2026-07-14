using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Security;

namespace MothersonBoxManagement.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Box> Boxes => Set<Box>();
    public DbSet<BoxPackage> BoxPackages => Set<BoxPackage>();
    public DbSet<BoxAuditLog> BoxAuditLogs => Set<BoxAuditLog>();
    public DbSet<BoxPrintJob> BoxPrintJobs => Set<BoxPrintJob>();
    public DbSet<BoxTemplate> BoxTemplates => Set<BoxTemplate>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<PrinterConfiguration> PrinterConfigurations => Set<PrinterConfiguration>();
    public DbSet<BarcodeConfiguration> BarcodeConfigurations => Set<BarcodeConfiguration>();
    public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
    public DbSet<PrintAgentPairingCode> PrintAgentPairingCodes => Set<PrintAgentPairingCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Matricule).IsUnique();
            entity.Property(u => u.Matricule).HasMaxLength(20);
            entity.Property(u => u.FullName).HasMaxLength(200);
            entity.Property(u => u.Role).HasMaxLength(40);
            entity.Property(u => u.SecurityStamp).HasMaxLength(64);
            entity.HasData(new User
            {
                Id = SystemPrincipal.UserId,
                Matricule = SystemPrincipal.Matricule,
                FullName = "Application System",
                PasswordHash = "LOGIN-DISABLED",
                Role = SystemPrincipal.Role,
                IsActive = false,
                SecurityStamp = "SYSTEM-PRINCIPAL",
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });

        builder.Entity<Box>(entity =>
        {
            entity.ToTable("Boxes", table =>
            {
                table.HasCheckConstraint(
                    "CK_Boxes_ExpectedQuantity_Positive",
                    "[ExpectedQuantity] > 0");
                table.HasCheckConstraint(
                    "CK_Boxes_CurrentQuantity_Range",
                    "[CurrentQuantity] >= 0 AND [CurrentQuantity] <= [ExpectedQuantity]");
                table.HasCheckConstraint(
                    "CK_Boxes_Dimensions_Positive",
                    "[Height] > 0 AND [Width] > 0 AND [Depth] > 0");
            });

            entity.HasIndex(b => b.BoxNumber).IsUnique();
            entity.HasIndex(b => b.BarcodeValue).IsUnique();
            entity.Property(b => b.RowVersion).IsRowVersion();
            entity.Property(b => b.BoxNumber).HasMaxLength(80);
            entity.Property(b => b.BarcodeValue).HasMaxLength(100);
            entity.Property(b => b.CompletionMode).HasMaxLength(40);
            entity.Property(b => b.ExceptionReason).HasMaxLength(500);
            entity.Property(b => b.BlockReason).HasMaxLength(500);

            entity.Property(b => b.Height).HasColumnType("decimal(10,2)");
            entity.Property(b => b.Width).HasColumnType("decimal(10,2)");
            entity.Property(b => b.Depth).HasColumnType("decimal(10,2)");

            entity.HasOne(b => b.CreatedBy)
                .WithMany(u => u.CreatedBoxes)
                .HasForeignKey(b => b.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.LastModifiedBy)
                .WithMany(u => u.ModifiedBoxes)
                .HasForeignKey(b => b.LastModifiedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.CompletedBy)
                .WithMany()
                .HasForeignKey(b => b.CompletedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.BlockedBy)
                .WithMany()
                .HasForeignKey(b => b.BlockedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BoxPackage>(entity =>
        {
            entity.Property(bp => bp.PackageBarcode).HasMaxLength(100);
            entity.Property(bp => bp.WorkstationName).HasMaxLength(64);
            entity.Property(bp => bp.BlockReason).HasMaxLength(500);
            entity.Property(bp => bp.RemovalReason).HasMaxLength(500);
            entity.Property(bp => bp.ScanRequestId).HasMaxLength(64);
            entity.HasIndex(bp => bp.ScanRequestId)
                .IsUnique()
                .HasFilter("[ScanRequestId] IS NOT NULL");
            entity.HasIndex(bp => bp.PackageBarcode)
                .IsUnique()
                .HasFilter("[IsRemoved] = 0");

            entity.HasOne(bp => bp.Box)
                .WithMany(b => b.Packages)
                .HasForeignKey(bp => bp.BoxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(bp => bp.ScannedBy)
                .WithMany(u => u.ScannedPackages)
                .HasForeignKey(bp => bp.ScannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(bp => bp.RemovedBy)
                .WithMany()
                .HasForeignKey(bp => bp.RemovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BoxAuditLog>(entity =>
        {
            entity.Property(log => log.PackageBarcode).HasMaxLength(100);
            entity.Property(log => log.ActionType).HasMaxLength(80);
            entity.Property(log => log.WorkstationName).HasMaxLength(64);
            entity.Property(log => log.PreviousValue).HasMaxLength(1000);
            entity.Property(log => log.NewValue).HasMaxLength(1000);
            entity.Property(log => log.Reason).HasMaxLength(1000);
            entity.Property(log => log.Description).HasMaxLength(1000);
            entity.Property(log => log.DetailsJson).HasColumnType("nvarchar(max)");
            entity.HasOne(al => al.Box)
                .WithMany()
                .HasForeignKey(al => al.BoxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BoxPrintJob>(entity =>
        {
            entity.HasOne(pj => pj.Box)
                .WithMany()
                .HasForeignKey(pj => pj.BoxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pj => pj.RequestedBy)
                .WithMany()
                .HasForeignKey(pj => pj.RequestedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(pj => pj.Workstation)
                .WithMany()
                .HasForeignKey(pj => pj.WorkstationId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(pj => pj.PrinterName).HasMaxLength(200);
            entity.Property(pj => pj.PrinterUncPath).HasMaxLength(500);
            entity.Property(pj => pj.PayloadType).HasMaxLength(20);
            entity.Property(pj => pj.Status).HasMaxLength(40);
            entity.Property(pj => pj.ErrorMessage).HasMaxLength(500);
            entity.Property(pj => pj.ReprintReason).HasMaxLength(500);
            entity.Property(pj => pj.LeaseTokenHash).HasMaxLength(64);
            entity.Property(pj => pj.RowVersion).IsRowVersion();
        });

        builder.Entity<PrinterConfiguration>(entity =>
        {
            entity.HasIndex(pc => pc.Code).IsUnique();
            entity.Property(pc => pc.Code).HasMaxLength(50);
            entity.Property(pc => pc.PcName).HasMaxLength(100);
            entity.Property(pc => pc.PrinterName).HasMaxLength(200);
            entity.Property(pc => pc.PrinterUncPath).HasMaxLength(500);
            entity.Property(pc => pc.PrintMode).HasMaxLength(20);
            entity.Property(pc => pc.MachineName).HasMaxLength(100);
            entity.Property(pc => pc.AgentTokenHash).HasMaxLength(64);
            entity.HasIndex(pc => pc.AgentTokenHash).IsUnique().HasFilter("[AgentTokenHash] IS NOT NULL");
            entity.Property(pc => pc.AgentVersion).HasMaxLength(40);
            entity.Property(pc => pc.AvailablePrintersJson).HasColumnType("nvarchar(max)");
            entity.Property(pc => pc.Description).HasMaxLength(500);
        });

        builder.Entity<PrintAgentPairingCode>(entity =>
        {
            entity.HasIndex(code => code.CodeHash).IsUnique();
            entity.Property(code => code.CodeHash).HasMaxLength(64);
            entity.HasOne(code => code.Workstation)
                .WithMany()
                .HasForeignKey(code => code.WorkstationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BoxTemplate>(entity =>
        {
            entity.HasIndex(bt => bt.Name).IsUnique();
            entity.HasIndex(bt => bt.PackagePrefixPattern)
                .IsUnique()
                .HasFilter("[IsActive] = 1 AND [PackagePrefixPattern] IS NOT NULL");

            entity.Property(bt => bt.Name).HasMaxLength(200);
            entity.Property(bt => bt.Description).HasMaxLength(1000);
            entity.Property(bt => bt.Height).HasColumnType("decimal(10,2)");
            entity.Property(bt => bt.Width).HasColumnType("decimal(10,2)");
            entity.Property(bt => bt.Depth).HasColumnType("decimal(10,2)");

            entity.HasOne(bt => bt.CreatedBy)
                .WithMany()
                .HasForeignKey(bt => bt.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LoginAttempt>(entity =>
        {
            entity.HasIndex(la => la.Matricule).IsUnique();
        });

        builder.Entity<PasswordResetRequest>(entity =>
        {
            entity.Property(request => request.Status).HasMaxLength(20);
            entity.Property(request => request.RequestedFromIp).HasMaxLength(64);
            entity.Property(request => request.RowVersion).IsRowVersion();
            entity.HasIndex(request => new { request.UserId, request.Status });
            entity.HasOne(request => request.User)
                .WithMany()
                .HasForeignKey(request => request.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(request => request.ApprovedByUser)
                .WithMany()
                .HasForeignKey(request => request.ApprovedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BarcodeConfiguration>(entity =>
        {
            entity.Property(bc => bc.BoxPrefix).HasMaxLength(20);
            entity.Property(bc => bc.BoxDatePattern).HasMaxLength(20);
            entity.Property(bc => bc.PackagePrefix).HasMaxLength(20);

            entity.HasData(new BarcodeConfiguration
            {
                Id = 1,
                BoxPrefix = "BOX-",
                BoxDatePattern = "yyyyMMdd",
                BoxRandomLength = 6,
                PackagePrefix = null,
                PackageMinLength = 3,
                CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        });
    }
}
