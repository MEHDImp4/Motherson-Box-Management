namespace MothersonBoxManagement.Entities;

public class User
{
    public int Id { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool MustChangePassword { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Box> CreatedBoxes { get; set; } = new List<Box>();
    public ICollection<Box> ModifiedBoxes { get; set; } = new List<Box>();
    public ICollection<BoxPackage> ScannedPackages { get; set; } = new List<BoxPackage>();
    public ICollection<BoxAuditLog> AuditLogs { get; set; } = new List<BoxAuditLog>();
}
