using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Entities;

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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Matricule).IsUnique();
        });

        builder.Entity<Box>(entity =>
        {
            entity.HasIndex(b => b.BoxNumber).IsUnique();
            entity.HasIndex(b => b.BarcodeValue).IsUnique();
            entity.Property(b => b.RowVersion).IsRowVersion();

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
            entity.HasIndex(bp => bp.PackageBarcode).IsUnique();

            entity.HasOne(bp => bp.Box)
                .WithMany(b => b.Packages)
                .HasForeignKey(bp => bp.BoxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(bp => bp.ScannedBy)
                .WithMany(u => u.ScannedPackages)
                .HasForeignKey(bp => bp.ScannedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BoxAuditLog>(entity =>
        {
            entity.HasOne(al => al.Box)
                .WithMany()
                .HasForeignKey(al => al.BoxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(al => al.RelatedBox)
                .WithMany()
                .HasForeignKey(al => al.RelatedBoxId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(al => al.User)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
