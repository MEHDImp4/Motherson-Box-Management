using MothersonBoxManagement.Dtos;

namespace MothersonBoxManagement.Services;

public interface IBoxLifecycleService
{
    Task<BoxDetailsDto> CreateBoxAsync(CreateBoxDto dto, int userId, CancellationToken cancellationToken = default);
    Task<BoxDetailsDto> OpenBoxAsync(int boxId, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> CancelBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> ForceCloseBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> UpdateExpectedQuantityAsync(int boxId, int newQuantity, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> BlockBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> UnblockBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
    Task<BoxDetailsDto> ArchiveBoxAsync(int boxId, string reason, int userId, string workstationName, CancellationToken ct = default);
}
