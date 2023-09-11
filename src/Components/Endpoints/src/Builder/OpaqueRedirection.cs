// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

internal partial class OpaqueRedirection
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
    //    and client-side code will then:
    //    - Check if it went to a Blazor endpoint, and if so, simply update the client-side URL to match
    //    - Or if it's a non-Blazor endpoint, behaves like "external URL" below
    //  - If it's to an external URL:
    //    - If it's a GET request, the client-side code will retry as a non-enhanced request
    //    - For other request types, we have to let it fail as it would be unsafe to retry

    private const string RedirectionDataProtectionProviderPurpose = "Microsoft.AspNetCore.Components.Endpoints.OpaqueRedirection,V1";
    private const string RedirectionEndpointBaseRelativeUrl = "_framework/opaque-redirect";

    public static string CreateProtectedRedirectionUrl(HttpContext httpContext, string destinationUrl)
    {
        var protector = CreateProtector(httpContext);
        var options = httpContext.RequestServices.GetRequiredService<IOptions<RazorComponentsServiceOptions>>();
        var lifetime = options.Value.TemporaryRedirectionUrlValidityDuration;
        var protectedUrl = protector.Protect(destinationUrl, lifetime);
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
            string url;

            try
            {
                url = protector.Unprotect(protectedUrl[0]!);
            }
            catch (CryptographicException ex)
            {
                if (httpContext.RequestServices.GetService<ILogger<OpaqueRedirection>>() is { } logger)
                {
                    Log.OpaqueUrlUnprotectionFailed(logger, ex);
                }

                httpContext.Response.StatusCode = 400;
                return Task.CompletedTask;
            }

            httpContext.Response.Redirect(url);
            return Task.CompletedTask;
        });
    }

    private static ITimeLimitedDataProtector CreateProtector(HttpContext httpContext)
    {
        var dataProtectionProvider = httpContext.RequestServices.GetRequiredService<IDataProtectionProvider>();
        return dataProtectionProvider.CreateProtector(RedirectionDataProtectionProviderPurpose).ToTimeLimitedDataProtector();
    }

    public static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Opaque URL unprotection failed.", EventName = "OpaqueUrlUnprotectionFailed")]
        public static partial void OpaqueUrlUnprotectionFailed(ILogger<OpaqueRedirection> logger, Exception exception);
    }
}
