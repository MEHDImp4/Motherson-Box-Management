using MothersonBoxManagement.Entities;
using System;

namespace MothersonBoxManagement.Dtos;

public class BoxSearchFilterDto
{
    public string? BoxNumber { get; set; }
    public BoxStatus? Status { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
}
