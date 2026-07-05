using System.Linq.Expressions;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Data.Dtos;

public static class BoxMapper
{
    public static Expression<Func<Box, BoxDetailsDto>> ToDetailsDto()
    {
        return b => new BoxDetailsDto
        {
            Id = b.Id,
            BoxNumber = b.BoxNumber,
            BarcodeValue = b.BarcodeValue,
            Type = b.Type,
            Height = b.Height,
            Width = b.Width,
            Depth = b.Depth,
            ExpectedQuantity = b.ExpectedQuantity,
            CurrentQuantity = b.CurrentQuantity,
            Status = b.Status,
            CreatedByMatricule = b.CreatedBy.Matricule,
            CreatedAt = b.CreatedAt,
            ModifiedAt = b.ModifiedAt,
            CompletedAt = b.CompletedAt,
            CompletionMode = b.CompletionMode,
            ExceptionReason = b.ExceptionReason,
            BlockReason = b.BlockReason,
            BlockedAt = b.BlockedAt,
            Packages = b.Packages.Select(p => new PackageItemDto
            {
                Id = p.Id,
                PackageBarcode = p.PackageBarcode,
                ScannedAt = p.ScannedAt,
                ScannedByMatricule = p.ScannedBy.Matricule,
                IsBlocked = p.IsBlocked,
                BlockReason = p.BlockReason
            }).ToList()
        };
    }
}
