using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public interface IBoxTemplateService
{
    Task<List<BoxTemplateDto>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
    Task<BoxTemplateDto?> GetTemplateByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<BoxTemplateDto> CreateTemplateAsync(CreateBoxTemplateDto dto, int userId, CancellationToken cancellationToken = default);
    Task<BoxTemplateDto> UpdateTemplateAsync(int id, CreateBoxTemplateDto dto, CancellationToken cancellationToken = default);
    Task DeactivateTemplateAsync(int id, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto> CreateBoxFromTemplateAsync(int templateId, int userId, string workstationName, CancellationToken cancellationToken = default);
    Task<BoxTemplate?> FindTemplateByPackageBarcodeAsync(string packageBarcode, CancellationToken cancellationToken = default);
}
