using System.ComponentModel.DataAnnotations;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.ViewModels;

public class CreateBoxViewModel
{
    [Required(ErrorMessage = "Box type is required.")]
    public BoxType Type { get; set; }

    [Required(ErrorMessage = "Height is required.")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Height must be greater than 0.")]
    public decimal Height { get; set; }

    [Required(ErrorMessage = "Width is required.")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Width must be greater than 0.")]
    public decimal Width { get; set; }

    [Required(ErrorMessage = "Depth is required.")]
    [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "Depth must be greater than 0.")]
    public decimal Depth { get; set; }

    [Required(ErrorMessage = "Expected quantity is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
    public int ExpectedQuantity { get; set; }
}
