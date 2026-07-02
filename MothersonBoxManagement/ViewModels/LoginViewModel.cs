using System.ComponentModel.DataAnnotations;

namespace MothersonBoxManagement.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Le matricule est requis.")]
    [RegularExpression(@"^[a-zA-Z0-9]{3,20}$", ErrorMessage = "Le matricule doit contenir entre 3 et 20 caractères alphanumériques.")]
    public string Matricule { get; set; } = string.Empty;

    [Required(ErrorMessage = "Le mot de passe est requis.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
