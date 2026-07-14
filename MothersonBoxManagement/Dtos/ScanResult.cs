namespace MothersonBoxManagement.Dtos;

public class ScanResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public BoxDetailsDto? Box { get; set; }
}
