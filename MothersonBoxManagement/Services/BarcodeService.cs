using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Entities;
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

    private async Task<BarcodeConfiguration> GetConfigAsync(CancellationToken cancellationToken)
    {
        return await _context.BarcodeConfigurations
            .FirstOrDefaultAsync(bc => bc.Id == 1, cancellationToken)
            ?? new BarcodeConfiguration();
    }

    public async Task<string> GenerateUniqueBoxBarcodeAsync(CancellationToken cancellationToken = default)
    {
        var config = await GetConfigAsync(cancellationToken);
        string barcode;
        bool exists;
        int retries = 0;

        var randomMax = (long)Math.Pow(16, config.BoxRandomLength);
        var dateStr = DateTime.UtcNow.ToString(config.BoxDatePattern);

        do
        {
            var randomHex = Random.Shared.Next(0, (int)Math.Min(randomMax, int.MaxValue)).ToString($"X{config.BoxRandomLength}");
            barcode = $"{config.BoxPrefix}{dateStr}-{randomHex}";
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

    public async Task<bool> IsBoxBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigAsync(cancellationToken);
        return barcode.StartsWith(config.BoxPrefix, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsBoxBarcode(string barcode)
    {
        return barcode.StartsWith("BOX-", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<bool> IsPackageBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        var config = await GetConfigAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(barcode))
            return false;
        if (barcode.StartsWith(config.BoxPrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.IsNullOrEmpty(config.PackagePrefix) && !barcode.StartsWith(config.PackagePrefix, StringComparison.OrdinalIgnoreCase))
            return false;
        return barcode.Length >= config.PackageMinLength;
    }

    public bool IsPackageBarcode(string barcode)
    {
        return !string.IsNullOrWhiteSpace(barcode)
            && !IsBoxBarcode(barcode)
            && barcode.Length >= 3;
    }

    public async Task ValidateBarcodeFormatAsync(string barcode, bool expectBox, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode cannot be empty.");

        if (expectBox && !await IsBoxBarcodeAsync(barcode, cancellationToken))
            throw new InvalidOperationException("This is not a valid box barcode. Expected format starts with the configured box prefix.");

        if (!expectBox && await IsBoxBarcodeAsync(barcode, cancellationToken))
            throw new InvalidOperationException("Box barcodes cannot be scanned as packages.");
    }

    public void ValidateBarcodeFormat(string barcode, bool expectBox)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            throw new ArgumentException("Barcode cannot be empty.");

        if (expectBox && !IsBoxBarcode(barcode))
            throw new InvalidOperationException("This is not a valid box barcode. Expected format: BOX-...");

        if (!expectBox && IsBoxBarcode(barcode))
            throw new InvalidOperationException("Box barcodes cannot be scanned as packages.");
    }
}
