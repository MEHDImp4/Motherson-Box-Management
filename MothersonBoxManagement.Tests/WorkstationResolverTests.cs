using Microsoft.Extensions.Configuration;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Tests;

public class WorkstationResolverTests
{
    [Fact]
    public void Resolve_ClientWorkstationName_TakesPrecedence()
    {
        var resolver = CreateResolver("SERVER-STATION");

        var result = resolver.Resolve(" P3-STATION-01 ");

        Assert.Equal("P3-STATION-01", result);
    }

    [Fact]
    public void Resolve_ClientWorkstationName_IsLimitedToSixtyFourCharacters()
    {
        var resolver = CreateResolver("SERVER-STATION");
        var longStationName = new string('A', 80);

        var result = resolver.Resolve(longStationName);

        Assert.Equal(64, result.Length);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Resolve_MissingClientWorkstationName_FallsBackToConfiguredStation(string? clientWorkstationName)
    {
        var resolver = CreateResolver("SERVER-STATION");

        var result = resolver.Resolve(clientWorkstationName);

        Assert.Equal("SERVER-STATION", result);
    }

    private static WorkstationResolver CreateResolver(string configuredStationName)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WorkstationName"] = configuredStationName
            })
            .Build();

        return new WorkstationResolver(configuration);
    }
}
