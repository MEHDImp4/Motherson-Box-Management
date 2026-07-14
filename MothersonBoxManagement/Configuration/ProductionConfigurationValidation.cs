namespace MothersonBoxManagement.Configuration;

public static class ProductionConfigurationValidation
{
    public static void ValidateProductionConfiguration(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsProduction())
            return;

        if (builder.Configuration.GetValue<bool>("SeedDemoUsers"))
            throw new InvalidOperationException(
                "SeedDemoUsers must never be enabled in Production.");

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("DefaultConnection")))
            missing.Add("ConnectionStrings:DefaultConnection");
        if (string.IsNullOrWhiteSpace(builder.Configuration["AllowedHosts"]))
            missing.Add("AllowedHosts");
        if (string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Path"]))
            missing.Add("Kestrel:Certificates:Default:Path");
        if (string.IsNullOrWhiteSpace(builder.Configuration["Kestrel:Certificates:Default:Password"]))
            missing.Add("Kestrel:Certificates:Default:Password");
        if (string.IsNullOrWhiteSpace(builder.Configuration["DataProtection:KeyPath"]))
            missing.Add("DataProtection:KeyPath");
        if (string.IsNullOrWhiteSpace(builder.Configuration["DataProtection:CertificatePath"]))
            missing.Add("DataProtection:CertificatePath");
        if (string.IsNullOrWhiteSpace(builder.Configuration["DataProtection:CertificatePassword"]))
            missing.Add("DataProtection:CertificatePassword");

        if (missing.Count > 0)
            throw new InvalidOperationException(
                "Production configuration is incomplete: " + string.Join(", ", missing));
    }
}
