namespace MothersonBoxManagement.Services;

public interface IWorkstationResolver
{
    string Resolve();
}

public class WorkstationResolver : IWorkstationResolver
{
    private readonly IConfiguration _configuration;

    public WorkstationResolver(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Resolve()
    {
        string? configured = _configuration["WorkstationName"];
        if (string.IsNullOrWhiteSpace(configured) || configured == "DEV-STATION-01" || configured == "DEFAULT-STATION")
        {
            return Environment.MachineName;
        }
        return configured;
    }
}
