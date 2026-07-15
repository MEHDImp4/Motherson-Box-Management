using MothersonBoxManagement.Dtos;

namespace MothersonBoxManagement.Models;

public sealed class PackageSearchPageViewModel
{
    public string? Query { get; set; }
    public string? Error { get; set; }
    public string? SearchedBarcode { get; set; }
    public BoxDetailsDto? FoundBox { get; set; }
    public IReadOnlyList<PackageSearchListItem> Packages { get; set; } = Array.Empty<PackageSearchListItem>();
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalItems { get; set; }
    public int TotalPages { get; set; } = 1;
    public int RangeStart => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int RangeEnd => Math.Min(CurrentPage * PageSize, TotalItems);
}

public sealed class PackageSearchListItem
{
    public string Barcode { get; set; } = string.Empty;
    public string BoxNumber { get; set; } = string.Empty;
    public string BoxBarcode { get; set; } = string.Empty;
    public string BoxStatus { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public bool IsBlocked { get; set; }
    public bool IsRemoved { get; set; }
}
