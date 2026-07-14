namespace MothersonBoxManagement.Dtos;

public class AuditFilterDto
{
    public int? BoxId { get; set; }
    public string? ActionType { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
