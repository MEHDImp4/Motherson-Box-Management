using System.ComponentModel.DataAnnotations;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Data.Dtos;

public class CreateBoxDto
{
    public BoxType Type { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
    public int Depth { get; set; }
    public int ExpectedQuantity { get; set; }
}
