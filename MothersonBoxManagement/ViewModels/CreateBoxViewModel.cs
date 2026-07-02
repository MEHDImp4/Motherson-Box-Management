using System.ComponentModel.DataAnnotations;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.ViewModels;

public class CreateBoxViewModel
{
    [Required(ErrorMessage = "Le type de box est requis.")]
    public BoxType Type { get; set; }

    [Required(ErrorMessage = "La hauteur est requise.")]
    [Range(1, int.MaxValue, ErrorMessage = "La hauteur doit être supérieure à 0.")]
    public int Height { get; set; }

    [Required(ErrorMessage = "La largeur est requise.")]
    [Range(1, int.MaxValue, ErrorMessage = "La largeur doit être supérieure à 0.")]
    public int Width { get; set; }

    [Required(ErrorMessage = "La profondeur est requise.")]
    [Range(1, int.MaxValue, ErrorMessage = "La profondeur doit être supérieure à 0.")]
    public int Depth { get; set; }

    [Required(ErrorMessage = "La quantité attendue est requise.")]
    [Range(1, int.MaxValue, ErrorMessage = "La quantité doit être au moins 1.")]
    public int ExpectedQuantity { get; set; }
}
