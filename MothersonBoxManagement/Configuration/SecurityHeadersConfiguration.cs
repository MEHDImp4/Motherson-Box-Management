using MothersonBoxManagement.Security;

namespace MothersonBoxManagement.Configuration;

public static class SecurityHeadersConfiguration
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            context.Response.Headers.Append("X-Frame-Options", "DENY");
            context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.Append("X-XSS-Protection", "0");
            context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");

            var nonceService = context.RequestServices.GetService<ICspNonceService>();
            var nonce = nonceService?.GetNonce(context) ?? string.Empty;
            context.Response.Headers.Append(
                "Content-Security-Policy",
                $"default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none'; img-src 'self' data:; font-src 'self'; style-src 'self' 'unsafe-inline'; script-src 'self' 'nonce-{nonce}'; connect-src 'self'; form-action 'self'");
            await next();
        });

        return app;
    }
}
