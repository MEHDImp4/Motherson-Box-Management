using System.Collections.Generic;
using MothersonBoxManagement.Dtos;

namespace MothersonBoxManagement.Models;

public class TemplateSelectionViewModel
{
    public string? Role { get; set; }
    public IReadOnlyList<BoxTemplateDto> Templates { get; set; } = new List<BoxTemplateDto>();
}
