using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MothersonBoxManagement.Data;
using MothersonBoxManagement.Dtos;
using MothersonBoxManagement.Entities;
using MothersonBoxManagement.Services;
using MothersonBoxManagement.Models;
using Xunit;

namespace MothersonBoxManagement.Tests;

public class AdditionalTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AdditionalTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Test Template", BoxType.Cardboard, 10, 10, 10, 5, true)]
    [InlineData("Plastic Box", BoxType.Plastic, 1, 1, 1, 1, true)]
    [InlineData("Wood Crate", BoxType.Wood, 9999, 9999, 9999, 9999, true)]
    [InlineData("Zero Height", BoxType.Cardboard, 0, 10, 10, 5, false)]
    [InlineData("Negative H", BoxType.Cardboard, -1, 10, 10, 5, false)]
    [InlineData("Negative Big H", BoxType.Cardboard, -9999, 10, 10, 5, false)]
    [InlineData("Zero Width", BoxType.Cardboard, 10, 0, 10, 5, false)]
    [InlineData("Negative W", BoxType.Cardboard, 10, -5, 10, 5, false)]
    [InlineData("Zero Depth", BoxType.Cardboard, 10, 10, 0, 5, false)]
    [InlineData("Negative D", BoxType.Cardboard, 10, 10, -100, 5, false)]
    [InlineData("Zero Qty", BoxType.Cardboard, 10, 10, 10, 0, false)]
    [InlineData("Negative Qty", BoxType.Cardboard, 10, 10, 10, -1, false)]
    [InlineData("Neg Big Qty", BoxType.Cardboard, 10, 10, 10, -500, false)]
    public void BoxTemplateViewModel_Validation_Theories(string name, BoxType type, int height, int width, int depth, int expectedQuantity, bool expectedIsValid)
    {
        // Arrange
        var model = new BoxTemplateViewModel
        {
            Name = name,
            Type = type,
            Height = height,
            Width = width,
            Depth = depth,
            ExpectedQuantity = expectedQuantity
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }

    [Theory]
    [InlineData("OP001", "password", true)]
    [InlineData("SP001", "Motherson2026!", true)]
    [InlineData("abc", "123", true)]
    [InlineData("ab", "password", false)]
    [InlineData("123456789012345678901", "password", false)]
    [InlineData("OP-001", "password", false)]
    [InlineData("", "password", false)]
    [InlineData("   ", "password", false)]
    [InlineData("OP001", "", false)]
    [InlineData("OP001", "   ", false)]
    [InlineData("", "", false)]
    public void LoginViewModel_Validation_Theories(string matricule, string password, bool expectedIsValid)
    {
        // Arrange
        var model = new LoginViewModel
        {
            Matricule = matricule,
            Password = password
        };

        var validationContext = new ValidationContext(model);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(model, validationContext, validationResults, true);

        // Assert
        Assert.Equal(expectedIsValid, isValid);
    }

    [Theory]
    [InlineData("Motherson2026!", "Motherson2026!", true)]
    [InlineData("Motherson2026!", "motherson2026!", false)]
    [InlineData("Password123", "Password123", true)]
    [InlineData("Password123", "WrongPassword", false)]
    [InlineData("AdminPassword", "AdminPassword", true)]
    [InlineData("AdminPassword", "", false)]
    [InlineData("", "", true)]
    [InlineData("   ", "   ", true)]
    [InlineData("Short", "Short", true)]
    [InlineData("SuperLongPasswordThatIsVerySecureAndHasManyCharacters12345!", "SuperLongPasswordThatIsVerySecureAndHasManyCharacters12345!", true)]
    public void PasswordHashing_Theories(string passwordToHash, string passwordToVerify, bool expectedResult)
    {
        // Arrange
        var user = new User { Matricule = "TEST01", Role = "Operator" };
        var hasher = new PasswordHasher<User>();
        var hash = hasher.HashPassword(user, passwordToHash);

        // Act
        var verifyResult = hasher.VerifyHashedPassword(user, hash, passwordToVerify);

        // Assert
        if (expectedResult)
        {
            Assert.True(verifyResult == PasswordVerificationResult.Success || verifyResult == PasswordVerificationResult.SuccessRehashNeeded);
        }
        else
        {
            Assert.Equal(PasswordVerificationResult.Failed, verifyResult);
        }
    }

    [Theory]
    [InlineData("BOX-123456", false, "Box barcodes cannot be scanned as packages.")]
    [InlineData("box-abc", false, "Box barcodes cannot be scanned as packages.")]
    [InlineData("BoX-XYZ", false, "Box barcodes cannot be scanned as packages.")]
    [InlineData("PKG-VALID", true, null)]
    [InlineData("123", true, null)]
    [InlineData("ABC", true, null)]
    [InlineData("BO-123", true, null)]
    [InlineData("PKG-DUP-99", true, null)]
    [InlineData("A-B-C", true, null)]
    [InlineData("12345-67890", true, null)]
    public async Task BoxService_BarcodeValidation_Theories(string barcode, bool expectedSuccess, string? expectedErrorMessage)
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var boxService = scope.ServiceProvider.GetRequiredService<IBoxService>();
        var packageScanService = scope.ServiceProvider.GetRequiredService<IPackageScanService>();

        var user = await db.Users.FirstAsync(u => u.Matricule == "OP001");
        
        var boxDto = new CreateBoxDto
        {
            Type = BoxType.Cardboard,
            Height = 10,
            Width = 10,
            Depth = 10,
            ExpectedQuantity = 10
        };
        var box = await boxService.CreateBoxAsync(boxDto, user.Id);
        await boxService.OpenBoxAsync(box.Id, user.Id, "TEST-STATION");

        // Act
        var result = await packageScanService.ScanPackageAsync(box.Id, barcode, user.Id, "TEST-STATION");

        // Assert
        Assert.Equal(expectedSuccess, result.Success);
        if (expectedErrorMessage != null)
        {
            Assert.Equal(expectedErrorMessage, result.Message);
        }
    }

    [Theory]
    [InlineData(BoxStatus.Open, "Open")]
    [InlineData(BoxStatus.Completed, "Completed")]
    [InlineData(BoxStatus.CompletedWithException, "CompletedWithException")]
    [InlineData(BoxStatus.Cancelled, "Cancelled")]
    [InlineData(BoxStatus.Archived, "Archived")]
    [InlineData(BoxStatus.Blocked, "Blocked")]
    [InlineData(BoxStatus.Created, "Created")]
    public void BoxStatus_EnumValues_Theories(BoxStatus status, string expectedName)
    {
        // Act & Assert
        Assert.Equal(expectedName, status.ToString());
    }
}
