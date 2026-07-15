using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Services;

namespace MothersonBoxManagement.Tests;

public class PasswordRecoveryFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PasswordRecoveryFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApprovedRequest_AllowsOnePasswordlessLogin_AndForcesPasswordChange()
    {
        var matricule = $"RC{Guid.NewGuid():N}"[..12].ToUpperInvariant();
        const string oldPassword = "Recovery-Old-2026!";
        const string newPassword = "Recovery-New-2026!";
        int requestId;

        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserService>();
            await users.CreateUserAsync(matricule, "Recovery Operator", "Operator", oldPassword);
        }

        var anonymous = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var requestResponse = await anonymous.PostAsync("/Account/RequestPasswordReset", Form(("matricule", matricule)));
        Assert.Equal(HttpStatusCode.Redirect, requestResponse.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var request = await db.PasswordResetRequests.SingleAsync(candidate => candidate.User.Matricule == matricule);
            Assert.Equal("Pending", request.Status);
            requestId = request.Id;
        }

        var admin = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");
        var approval = await admin.PostAsync("/Users/ApprovePasswordReset", Form(("requestId", requestId.ToString())));
        Assert.Equal(HttpStatusCode.Redirect, approval.StatusCode);

        var recovery = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var recoveryLogin = await recovery.PostAsync("/Account/RecoveryLogin", Form(("matricule", matricule)));
        Assert.Equal(HttpStatusCode.Redirect, recoveryLogin.StatusCode);
        Assert.Contains("/Account/ChangePassword", recoveryLogin.Headers.Location?.OriginalString ?? "");

        var changePage = await recovery.GetAsync("/Account/ChangePassword");
        Assert.Equal(HttpStatusCode.OK, changePage.StatusCode);
        Assert.Contains("Choose a new password", await changePage.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var forbiddenUntilChanged = await recovery.GetAsync("/");
        Assert.Equal(HttpStatusCode.Redirect, forbiddenUntilChanged.StatusCode);
        Assert.Contains("/Account/ChangePassword", forbiddenUntilChanged.Headers.Location?.OriginalString ?? "");

        var change = await recovery.PostAsync("/Account/ChangePassword", Form(
            ("newPassword", newPassword),
            ("confirmPassword", newPassword)));
        Assert.Equal(HttpStatusCode.Redirect, change.StatusCode);
        Assert.Contains("Dashboard", change.Headers.Location?.OriginalString ?? "", StringComparison.OrdinalIgnoreCase);

        using (var scope = _factory.Services.CreateScope())
        {
            var auth = scope.ServiceProvider.GetRequiredService<IUserAuthenticationService>();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            Assert.Null(await auth.ValidateCredentialsAsync(matricule, oldPassword, default));
            Assert.NotNull(await auth.ValidateCredentialsAsync(matricule, newPassword, default));
            Assert.Equal("Consumed", (await db.PasswordResetRequests.FindAsync(requestId))!.Status);
        }

        var replay = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var replayResponse = await replay.PostAsync("/Account/RecoveryLogin", Form(("matricule", matricule)));
        Assert.Equal(HttpStatusCode.OK, replayResponse.StatusCode);
        Assert.Contains("not approved", await replayResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetRequest_DoesNotRevealWhetherMatriculeExists()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var known = await client.PostAsync("/Account/RequestPasswordReset", Form(("matricule", "OP001")));
        var unknown = await client.PostAsync("/Account/RequestPasswordReset", Form(("matricule", "UNKNOWN999")));

        Assert.Equal(known.StatusCode, unknown.StatusCode);
        Assert.Equal(known.Headers.Location?.OriginalString, unknown.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Supervisor_CannotApprovePasswordResetRequest()
    {
        var supervisor = await TestAuthHelper.CreateAuthenticatedClient(_factory, "SP001", "Motherson2026!");
        var response = await supervisor.PostAsync("/Users/ApprovePasswordReset", Form(("requestId", "1")));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Account/Login", response.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Administrator_CanViewPendingPasswordResetRequests()
    {
        var admin = await TestAuthHelper.CreateAuthenticatedClient(_factory, "AD001", "Motherson2026!");

        var response = await admin.GetAsync("/Users/PasswordResetRequests");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Password Reset Requests", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }

    private static FormUrlEncodedContent Form(params (string Key, string Value)[] values) =>
        new(values.Select(value => new KeyValuePair<string, string>(value.Key, value.Value)));
}
