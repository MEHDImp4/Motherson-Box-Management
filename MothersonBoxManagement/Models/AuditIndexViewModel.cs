using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Models;

public class AuditIndexViewModel
{
    public List<BoxAuditLog> Items { get; set; } = new();
    public List<string> ActionTypes { get; set; } = new();
    public AuditFilterDto Filter { get; set; } = new();
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int TotalItems { get; set; }
    public int PageSize { get; set; }
}
