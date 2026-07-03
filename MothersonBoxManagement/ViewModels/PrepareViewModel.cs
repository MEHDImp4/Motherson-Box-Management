using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.ViewModels;

public class PrepareViewModel
{
    public BoxDetailsDto Box { get; set; } = null!;

    public bool IsBoxOpen => Box.Status == BoxStatus.Open;
    public bool IsCapacityReached => Box.CurrentQuantity >= Box.ExpectedQuantity;
    public int ProgressPercent => Box.ExpectedQuantity > 0 ? (int)((double)Box.CurrentQuantity / Box.ExpectedQuantity * 100) : 0;

    public string? ScanSuccessMessage { get; set; }
    public string? ScanErrorMessage { get; set; }
}
