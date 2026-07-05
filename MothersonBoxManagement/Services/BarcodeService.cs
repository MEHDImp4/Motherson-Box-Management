using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MothersonBoxManagement.Services;

public class BarcodeService : IBarcodeService
{
    private readonly ApplicationDbContext _context;

    public BarcodeService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> GenerateUniqueBoxBarcodeAsync(CancellationToken cancellationToken = default)
    {
        string barcode;
        bool exists;
        int retries = 0;

        do
        {
            barcode = $"BOX-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(0, 16777216):X6}";
            exists = await _context.Boxes.AnyAsync(
                b => b.BoxNumber == barcode || b.BarcodeValue == barcode,
                cancellationToken);
            retries++;
        }
        while (exists && retries < 10);

        if (exists)
        {
            throw new InvalidOperationException("Unable to generate a unique box ID after 10 attempts.");
        }

        return barcode;
    }

    public bool IsBoxBarcode(string barcode)
    {
        return barcode.StartsWith("BOX-", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsPackageBarcode(string barcode)
    {
        return !IsBoxBarcode(barcode);
    }

    public void ValidateBarcodeFormat(string barcode, bool expectBox)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode cannot be empty.");

        if (expectBox && !IsBoxBarcode(barcode))
            throw new InvalidOperationException("Ce code n'est pas un code-barres de box valide. Format attendu : BOX-...");

        if (!expectBox && IsBoxBarcode(barcode))
            throw new InvalidOperationException("Box barcodes cannot be scanned as packages.");
    }
}
