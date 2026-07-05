using System.ComponentModel.DataAnnotations;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Data.Dtos;

public class CreateBoxDto
{
    public BoxType Type { get; set; }
    public decimal Height { get; set; }
    public decimal Width { get; set; }
    public decimal Depth { get; set; }
    public int ExpectedQuantity { get; set; }
}
