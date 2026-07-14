namespace MothersonBoxManagement.Dtos;

public class UserListItemDto
{
    public int Id { get; set; }
    public string Matricule { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
