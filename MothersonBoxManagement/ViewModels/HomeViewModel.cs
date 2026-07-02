using Microsoft.AspNetCore.Mvc;
using MothersonBoxManagement.ViewModels;

namespace MothersonBoxManagement.Controllers;

public class HomeViewModel
{
    public string? Matricule { get; set; }
    public string? Role { get; set; }
    public string? Barcode { get; set; }
}
