using Microsoft.EntityFrameworkCore;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Printing;
using Microsoft.EntityFrameworkCore.Storage;

namespace MothersonBoxManagement.Services;

public class BoxTemplateService : IBoxTemplateService
{
    private readonly ApplicationDbContext _context;
    private readonly IBarcodeService _barcodeService;
    private readonly IPrintAgentService? _printAgentService;

    public BoxTemplateService(
        ApplicationDbContext context,
        IBarcodeService barcodeService,
        IPrintAgentService? printAgentService = null)
    {
        _context = context;
        _barcodeService = barcodeService;
        _printAgentService = printAgentService;
    }

    public async Task<List<BoxTemplateDto>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.BoxTemplates
            .Where(bt => bt.IsActive)
            .OrderBy(bt => bt.Name)
            .Select(bt => new BoxTemplateDto
            {
                Id = bt.Id,
                Name = bt.Name,
                Description = bt.Description,
                Type = bt.Type,
                Height = bt.Height,
                Width = bt.Width,
                Depth = bt.Depth,
                ExpectedQuantity = bt.ExpectedQuantity,
                PackagePrefixPattern = bt.PackagePrefixPattern,
                CreatedAt = bt.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BoxTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.BoxTemplates
            .Where(bt => bt.Id == id)
            .Select(bt => new BoxTemplateDto
            {
                Id = bt.Id,
                Name = bt.Name,
                Description = bt.Description,
                Type = bt.Type,
                Height = bt.Height,
                Width = bt.Width,
                Depth = bt.Depth,
                ExpectedQuantity = bt.ExpectedQuantity,
                PackagePrefixPattern = bt.PackagePrefixPattern,
                CreatedAt = bt.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BoxTemplateDto> CreateTemplateAsync(CreateBoxTemplateDto dto, int userId, CancellationToken cancellationToken = default)
    {
        var normalizedPrefix = NormalizePrefix(dto.PackagePrefixPattern);
        await EnsureActivePrefixAvailableAsync(normalizedPrefix, null, cancellationToken);
        var template = new BoxTemplate
        {
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            Height = dto.Height,
            Width = dto.Width,
            Depth = dto.Depth,
            ExpectedQuantity = dto.ExpectedQuantity,
            PackagePrefixPattern = normalizedPrefix,
            IsActive = true,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.BoxTemplates.Add(template);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetTemplateByIdAsync(template.Id, cancellationToken))
            ?? throw new InvalidOperationException("The template could not be saved.");
    }

    public async Task<BoxTemplateDto> UpdateTemplateAsync(int id, CreateBoxTemplateDto dto, CancellationToken cancellationToken = default)
    {
        var template = await _context.BoxTemplates
            .FirstOrDefaultAsync(bt => bt.Id == id, cancellationToken);

        if (template == null)
            throw new KeyNotFoundException($"Template with ID {id} was not found.");

        var normalizedPrefix = NormalizePrefix(dto.PackagePrefixPattern);
        await EnsureActivePrefixAvailableAsync(normalizedPrefix, id, cancellationToken);
        template.Name = dto.Name;
        template.Description = dto.Description;
        template.Type = dto.Type;
        template.Height = dto.Height;
        template.Width = dto.Width;
        template.Depth = dto.Depth;
        template.ExpectedQuantity = dto.ExpectedQuantity;
        template.PackagePrefixPattern = normalizedPrefix;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return (await GetTemplateByIdAsync(id, cancellationToken))
            ?? throw new InvalidOperationException("The template could not be saved.");
    }

    public async Task DeactivateTemplateAsync(int id, CancellationToken cancellationToken = default)
    {
        var template = await _context.BoxTemplates
            .FirstOrDefaultAsync(bt => bt.Id == id, cancellationToken);

        if (template == null)
            throw new KeyNotFoundException($"Template with ID {id} was not found.");

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<BoxDetailsDto> CreateBoxFromTemplateAsync(int templateId, int userId, string workstationName, CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? ownedTransaction = null;
        if (_context.Database.IsRelational() && _context.Database.CurrentTransaction is null)
            ownedTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
        var template = await _context.BoxTemplates
            .FirstOrDefaultAsync(bt => bt.Id == templateId, cancellationToken);

        if (template == null)
            throw new KeyNotFoundException($"Template with ID {templateId} was not found.");

        if (!template.IsActive)
            throw new InvalidOperationException("Cannot create a box from an inactive template.");

        var boxIdentifier = await _barcodeService.GenerateUniqueBoxBarcodeAsync(cancellationToken);

        var box = new Box
        {
            BoxNumber = boxIdentifier,
            BarcodeValue = boxIdentifier,
            Type = template.Type,
            Height = template.Height,
            Width = template.Width,
            Depth = template.Depth,
            ExpectedQuantity = template.ExpectedQuantity,
            CurrentQuantity = 0,
            Status = BoxStatus.Open,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            LastModifiedByUserId = userId,
            ModifiedAt = DateTime.UtcNow
        };

        _context.Boxes.Add(box);
        await _context.SaveChangesAsync(cancellationToken);

        var workstation = await _context.PrinterConfigurations.FirstOrDefaultAsync(candidate =>
            candidate.IsActive && candidate.RevokedAt == null &&
            (candidate.Code == workstationName || candidate.PcName == workstationName), cancellationToken);
        if (workstation is not null && _printAgentService is not null &&
            !string.IsNullOrWhiteSpace(workstation.PrinterName) && PrintModes.IsValid(workstation.PrintMode))
        {
            await _printAgentService.QueueAsync(box.Id, userId, workstation.Id, cancellationToken: cancellationToken);
        }

        if (ownedTransaction is not null)
            await ownedTransaction.CommitAsync(cancellationToken);

        return await _context.Boxes
            .Include(b => b.CreatedBy)
            .Include(b => b.Packages)
                .ThenInclude(p => p.ScannedBy)
            .Where(b => b.Id == box.Id)
            .Select(BoxMapper.ToDetailsDto())
            .FirstAsync(cancellationToken);
        }
        catch
        {
            if (ownedTransaction is not null)
                await ownedTransaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (ownedTransaction is not null)
                await ownedTransaction.DisposeAsync();
        }
    }

    public async Task<BoxTemplate?> FindTemplateByPackageBarcodeAsync(string packageBarcode, CancellationToken cancellationToken = default)
    {
        var candidates = await _context.BoxTemplates
            .AsNoTracking()
            .Where(bt => bt.IsActive && bt.PackagePrefixPattern != null && bt.PackagePrefixPattern != "")
            .Select(bt => new { bt.Id, bt.PackagePrefixPattern })
            .ToListAsync(cancellationToken);

        var bestMatch = candidates
            .Where(bt => packageBarcode.StartsWith(bt.PackagePrefixPattern!, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(bt => bt.PackagePrefixPattern!.Length)
            .FirstOrDefault();

        if (bestMatch is null)
            return null;

        return await _context.BoxTemplates.FindAsync(new object[] { bestMatch.Id }, cancellationToken);
    }

    private static string? NormalizePrefix(string? prefix) =>
        string.IsNullOrWhiteSpace(prefix) ? null : prefix.Trim().ToUpperInvariant();

    private async Task EnsureActivePrefixAvailableAsync(
        string? prefix,
        int? excludedTemplateId,
        CancellationToken cancellationToken)
    {
        if (prefix is null)
            return;
        var exists = await _context.BoxTemplates.AnyAsync(template =>
            template.IsActive &&
            template.PackagePrefixPattern == prefix &&
            (!excludedTemplateId.HasValue || template.Id != excludedTemplateId.Value),
            cancellationToken);
        if (exists)
            throw new InvalidOperationException($"An active template already uses package prefix '{prefix}'.");
    }
}
