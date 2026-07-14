using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Tests;

public class TemplateRemediationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TemplateRemediationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task ActiveTemplatePrefix_IsNormalizedAndCannotBeDuplicated()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IBoxTemplateService>();
        var user = await db.Users.SingleAsync(candidate => candidate.Matricule == "SP001");
        var prefix = $"cab{Guid.NewGuid():N}"[..12];
        var first = Template("  " + prefix.ToLowerInvariant() + "  ");
        var duplicate = Template(prefix.ToUpperInvariant());

        var created = await service.CreateTemplateAsync(first, user.Id);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateTemplateAsync(duplicate, user.Id));

        Assert.Equal(prefix.ToUpperInvariant(), created.PackagePrefixPattern);
        Assert.Contains("prefix", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static CreateBoxTemplateDto Template(string prefix) => new()
    {
        Name = $"Template-{Guid.NewGuid():N}",
        Type = BoxType.Cardboard,
        Height = 10,
        Width = 10,
        Depth = 10,
        ExpectedQuantity = 2,
        PackagePrefixPattern = prefix
    };
}
