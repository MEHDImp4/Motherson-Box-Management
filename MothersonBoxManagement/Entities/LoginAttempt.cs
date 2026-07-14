namespace MothersonBoxManagement.Entities;

public class LoginAttempt
{
    public int Id { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public int FailedAttempts { get; set; }
    public DateTime? LockoutEnd { get; set; }
    public DateTime LastAttemptAt { get; set; }
}
