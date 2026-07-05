using MothersonBoxManagement.Data.Dtos;
using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public interface IUserService
{
    Task<List<UserListItemDto>> GetAllUsersAsync(CancellationToken ct = default);
    Task<User?> GetUserByIdAsync(int id, CancellationToken ct = default);
    Task<User?> GetUserByMatriculeAsync(string matricule, CancellationToken ct = default);
    Task<User> CreateUserAsync(string matricule, string fullName, string role, string password, CancellationToken ct = default);
    Task<User> UpdateUserAsync(int id, string fullName, string role, bool isActive, CancellationToken ct = default);
    Task ResetPasswordAsync(int id, string newPassword, CancellationToken ct = default);
    Task DeleteUserAsync(int id, CancellationToken ct = default);
}
