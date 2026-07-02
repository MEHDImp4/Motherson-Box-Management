using MothersonBoxManagement.Entities;

namespace MothersonBoxManagement.Services;

public interface IUserAuthenticationService
{
    Task<User?> ValidateCredentialsAsync(string matricule, string password, CancellationToken cancellationToken);
}
