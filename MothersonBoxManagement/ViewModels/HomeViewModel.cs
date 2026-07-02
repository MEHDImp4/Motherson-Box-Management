using System.Collections.Generic;
using MothersonBoxManagement.Data.Dtos;

namespace MothersonBoxManagement.ViewModels;

public class HomeViewModel
{
    public string? Matricule { get; set; }
    public string? Role { get; set; }
    public string? Barcode { get; set; }
    public string? Error { get; set; }
    public string? Warning { get; set; }
    public IReadOnlyList<BoxListItemDto> OpenBoxes { get; set; } = new List<BoxListItemDto>();
}
