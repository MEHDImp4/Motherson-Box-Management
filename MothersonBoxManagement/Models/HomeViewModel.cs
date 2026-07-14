using System.Collections.Generic;
using MothersonBoxManagement.Dtos;
using System;

namespace MothersonBoxManagement.Models;

public class HomeViewModel
{
    public string? Matricule { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
    public string? Barcode { get; set; }
    public string? Error { get; set; }
    public string? Warning { get; set; }
    public IReadOnlyList<BoxListItemDto> OpenBoxes { get; set; } = new List<BoxListItemDto>();
    public IReadOnlyList<BoxListItemDto> CreatedBoxes { get; set; } = new List<BoxListItemDto>();
    public BoxDetailsDto? ScannedBox { get; set; }
    public int CurrentOpenBoxesPage { get; set; } = 1;
    public int OpenBoxesPageSize { get; set; } = 3;
    public int TotalOpenBoxesCount { get; set; }
    public int OpenBoxesTotalPages { get; set; }
    public bool HasPreviousOpenBoxesPage => CurrentOpenBoxesPage > 1;
    public bool HasNextOpenBoxesPage => CurrentOpenBoxesPage < OpenBoxesTotalPages;
    public int OpenBoxesRangeStart => TotalOpenBoxesCount == 0 ? 0 : ((CurrentOpenBoxesPage - 1) * OpenBoxesPageSize) + 1;
    public int OpenBoxesRangeEnd => TotalOpenBoxesCount == 0
        ? 0
        : Math.Min(CurrentOpenBoxesPage * OpenBoxesPageSize, TotalOpenBoxesCount);
}
