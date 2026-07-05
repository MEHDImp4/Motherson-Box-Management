using MothersonBoxManagement.Entities;
using System;

namespace MothersonBoxManagement.Data.Dtos;

public class BoxSearchFilterDto
{
    public string? BoxNumber { get; set; }
    public BoxStatus? Status { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
