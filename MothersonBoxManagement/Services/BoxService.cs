using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Services;

public class BoxService : IBoxService
{
    private readonly ApplicationDbContext _context;

    public BoxService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BoxDetailsDto> CreateBoxAsync(CreateBoxDto dto, int userId, CancellationToken cancellationToken = default)
    {
        string boxIdentifier = "";
        bool exists = true;
        int retries = 0;
        while (exists && retries < 10)
        {
            boxIdentifier = $"BOX-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(0, 16777216):X6}";
            exists = await _context.Boxes.AnyAsync(b => b.BoxNumber == boxIdentifier || b.BarcodeValue == boxIdentifier, cancellationToken);
            retries++;
        }
        if (exists)
        {
            throw new InvalidOperationException("Failed to generate a unique box identifier after 10 attempts.");
        }

        var box = new Box
        {
            BoxNumber = boxIdentifier,
            BarcodeValue = boxIdentifier,
            Type = dto.Type,
            Height = dto.Height,
            Width = dto.Width,
            Depth = dto.Depth,
            ExpectedQuantity = dto.ExpectedQuantity,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Boxes.Add(box);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetBoxByIdAsync(box.Id, cancellationToken)
            ?? throw new InvalidOperationException("Box was not persisted.");
    }

    public async Task<List<BoxListItemDto>> GetOpenBoxesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Where(b => b.Status == BoxStatus.Open)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new BoxListItemDto
            {
                Id = b.Id,
                BoxNumber = b.BoxNumber,
                BarcodeValue = b.BarcodeValue,
                Type = b.Type,
                ExpectedQuantity = b.ExpectedQuantity,
                CurrentQuantity = b.CurrentQuantity,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                UpdatedAt = b.UpdatedAt,
                CreatedByMatricule = b.CreatedBy.Matricule
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto?> GetBoxByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.BarcodeValue == barcode)
            .Select(MapToDetailsDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto?> GetBoxByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Boxes
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.Id == id)
            .Select(MapToDetailsDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ScanResult> ScanPackageAsync(int boxId, string barcode, int userId, CancellationToken cancellationToken = default)
    {
        if (barcode.StartsWith("BOX-", StringComparison.OrdinalIgnoreCase))
            return new ScanResult { Success = false, Message = "Les codes-barres de box ne peuvent pas être scannés comme paquets." };

        var isDuplicate = await _context.BoxPackages
            .AnyAsync(bp => bp.PackageBarcode == barcode, cancellationToken);

        if (isDuplicate)
            return new ScanResult { Success = false, Message = "Ce code-barres paquet a déjà été scanné." };

        var box = await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId, cancellationToken);

        if (box is null)
            return new ScanResult { Success = false, Message = "Box introuvable." };

        if (box.Status != BoxStatus.Open)
            return new ScanResult { Success = false, Message = "Cette box n'est pas ouverte aux scans." };

        if (box.CurrentQuantity >= box.ExpectedQuantity)
            return new ScanResult { Success = false, Message = "La quantité attendue est déjà atteinte." };

        var package = new BoxPackage
        {
            BoxId = boxId,
            PackageBarcode = barcode,
            ScannedByUserId = userId,
            ScannedAt = DateTime.UtcNow
        };

        _context.BoxPackages.Add(package);
        box.CurrentQuantity++;
        box.UpdatedAt = DateTime.UtcNow;
        box.LastModifiedByUserId = userId;

        if (box.CurrentQuantity >= box.ExpectedQuantity)
        {
            box.Status = BoxStatus.Completed;
            box.ClosedAt = DateTime.UtcNow;
            box.ClosedByUserId = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var updatedBox = await GetBoxByIdAsync(boxId, cancellationToken);
        var msg = box.Status == BoxStatus.Completed
            ? "Scan réussi ! Box complétée automatiquement."
            : $"Scan réussi ! {box.CurrentQuantity}/{box.ExpectedQuantity} paquets.";

        return new ScanResult { Success = true, Message = msg, Box = updatedBox };
    }

    private static System.Linq.Expressions.Expression<System.Func<Box, BoxDetailsDto>> MapToDetailsDto()
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
            UpdatedAt = b.UpdatedAt,
            ClosedAt = b.ClosedAt,
            Packages = b.Packages.Select(p => new PackageItemDto
            {
                Id = p.Id,
                PackageBarcode = p.PackageBarcode,
                ScannedAt = p.ScannedAt,
                ScannedByMatricule = p.ScannedBy.Matricule
            }).ToList()
        };
    }
}
