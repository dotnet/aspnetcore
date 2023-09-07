// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal static class OpaqueRedirection
{
    // During streaming SSR, a component may try to perform a redirection. Since the response has already started
    // this can only work if we communicate the redirection back via some command that can get handled by JS,
    // rather than a true 301/302/etc. But we don't want to disclose the redirection target URL to JS because that
    // info would not normally be available, e.g., when using 'fetch'. So we data-protect the URL and round trip
    // through a special endpoint that can issue a true redirection.
    //
    // The same is used during enhanced navigation if it happens to go to a Blazor endpoint that calls
    // NavigationManager.NavigateTo, for the same reasons.
    //
    // However, if enhanced navigation goes to a non-Blazor endpoint, the server won't do anything special and just
    // returns a regular 301/302/etc. To handle this,
    //
    //  - If it's redirected to an internal URL, the browser will just follow the redirection automatically
    //    and client-side code simply updates the client-side URL to match
    //  - If it's to an external URL:
    //    - If it's a GET request, the client-side code will retry as a non-enhanced request
    //    - For other request types, we have to let it fail as it would be unsafe to retry

    private const string RedirectionDataProtectionProviderPurpose = "Microsoft.AspNetCore.Components.Endpoints.OpaqueRedirection,V1";
    private const string RedirectionEndpointBaseRelativeUrl = "_framework/opaque-redirect";

    public static string CreateProtectedRedirectionUrl(HttpContext httpContext, string destinationUrl)
    {
        // For consistency with how 'fetch' works, we don't want to disclose the redirection URL to JS code, even if
        // it's an internal URL (e.g., if the redirection chain is A->B->C, we don't want JS to be able to see B).
        // This is even more important if it's an external URL, since it could be an auth endpoint with sensitive
        // info inside it. So, we supply the URL to a framework endpoint that will perform the redirection, and
        // the client-side code and navigate to this.
        var protector = CreateProtector(httpContext);
        var protectedUrl = protector.Protect(destinationUrl, TimeSpan.FromSeconds(10));
        return $"{RedirectionEndpointBaseRelativeUrl}?url={UrlEncoder.Default.Encode(protectedUrl)}";
    }

    public static void AddBlazorOpaqueRedirectionEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet($"/{RedirectionEndpointBaseRelativeUrl}", httpContext =>
        {
            if (!httpContext.Request.Query.TryGetValue("url", out var protectedUrl))
            {
                httpContext.Response.StatusCode = 400;
                return Task.CompletedTask;
            }

            var protector = CreateProtector(httpContext);
            var url = protector.Unprotect(protectedUrl[0]!);
            httpContext.Response.Redirect(url);
            return Task.CompletedTask;
        });
    }

    private static ITimeLimitedDataProtector CreateProtector(HttpContext httpContext)
    {
        var dataProtectionProvider = httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>();
        return dataProtectionProvider.CreateProtector(RedirectionDataProtectionProviderPurpose).ToTimeLimitedDataProtector();
    }
}
