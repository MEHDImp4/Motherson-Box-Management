using System.ComponentModel.DataAnnotations;

namespace MothersonBoxManagement.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Matricule is required.")]
    [RegularExpression(@"^[a-zA-Z0-9]{3,20}$", ErrorMessage = "Matricule must contain 3 to 20 alphanumeric characters.")]
    public string Matricule { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}
