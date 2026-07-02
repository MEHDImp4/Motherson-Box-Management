using System.Net.Http.Headers;
using MothersonBoxManagement.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;

namespace MothersonBoxManagement.Tests;

public static class TestAuthHelper
{
    public static async Task<HttpClient> CreateAuthenticatedClient(CustomWebApplicationFactory factory, string matricule, string password)
    {
        var client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("Matricule", matricule),
            new KeyValuePair<string, string>("Password", password)
        });

        var loginResponse = await client.PostAsync("/Account/Login", formData);
        loginResponse.EnsureSuccessStatusCode();

        return client;
    }

    public static StringContent ToJsonContent<T>(this T obj)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(obj);
        return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
    }
}
