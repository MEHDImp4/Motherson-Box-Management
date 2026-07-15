using Microsoft.AspNetCore.Razor.TagHelpers;
using MothersonBoxManagement.Security;

namespace MothersonBoxManagement.TagHelpers;

[HtmlTargetElement("script", Attributes = "csp-nonce")]
public sealed class CspNonceTagHelper : TagHelper
{
    private readonly ICspNonceService _nonceService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CspNonceTagHelper(ICspNonceService nonceService, IHttpContextAccessor httpContextAccessor)
    {
        _nonceService = nonceService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is not null)
        {
            var nonce = _nonceService.GetNonce(httpContext);
            output.Attributes.SetAttribute("nonce", nonce);
        }
        output.Attributes.RemoveAll("csp-nonce");
    }
}
