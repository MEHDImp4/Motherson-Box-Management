using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Dtos;

public class CreateBoxTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public BoxType Type { get; set; }
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Depth { get; set; }
    public int ExpectedQuantity { get; set; }
    public string? PackagePrefixPattern { get; set; }
}
