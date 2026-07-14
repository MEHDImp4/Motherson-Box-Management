using System.ComponentModel.DataAnnotations;

namespace MothersonBoxManagement.Models;

public class PasswordResetRequestViewModel
{
    [Required(ErrorMessage = "Matricule is required.")]
    [RegularExpression(@"^[a-zA-Z0-9]{3,20}$", ErrorMessage = "Matricule must contain 3 to 20 alphanumeric characters.")]
    public string Matricule { get; set; } = string.Empty;
}

public class RecoveryLoginViewModel
{
    [Required(ErrorMessage = "Matricule is required.")]
    [RegularExpression(@"^[a-zA-Z0-9]{3,20}$", ErrorMessage = "Matricule must contain 3 to 20 alphanumeric characters.")]
    public string Matricule { get; set; } = string.Empty;
}

public class ForcedPasswordChangeViewModel
{
    [Required(ErrorMessage = "New password is required.")]
    [StringLength(128, MinimumLength = 12, ErrorMessage = "Password must contain at least 12 characters.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
