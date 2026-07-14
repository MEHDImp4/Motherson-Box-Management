using System.ComponentModel.DataAnnotations;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Models;

public class BoxTemplateViewModel
{
    [Required(ErrorMessage = "Template name is required.")]
    [StringLength(200, ErrorMessage = "Name must not exceed 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description must not exceed 1000 characters.")]
    public string? Description { get; set; }

    [StringLength(50, ErrorMessage = "Prefix pattern must not exceed 50 characters.")]
    [RegularExpression(@"^[0-9]*$", ErrorMessage = "Prefix pattern must contain only digits.")]
    public string? PackagePrefixPattern { get; set; }

    [Required(ErrorMessage = "Box type is required.")]
    public BoxType Type { get; set; }

    [Required(ErrorMessage = "Height is required.")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Height must be at least 1 cm.")]
    public decimal Height { get; set; }

    [Required(ErrorMessage = "Width is required.")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Width must be at least 1 cm.")]
    public decimal Width { get; set; }

    [Required(ErrorMessage = "Depth is required.")]
    [Range(1, (double)decimal.MaxValue, ErrorMessage = "Depth must be at least 1 cm.")]
    public decimal Depth { get; set; }

    [Required(ErrorMessage = "Expected quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int ExpectedQuantity { get; set; }
}
