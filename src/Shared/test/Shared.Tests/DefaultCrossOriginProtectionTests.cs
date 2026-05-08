// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Shared.Tests;

public class DefaultCrossOriginProtectionTests
{
    private readonly DefaultCrossOriginProtection _validator = new();

    private static HttpContext CreateContext(
        string method = "POST",
        string? secFetchSite = null,
        string? origin = null,
        string scheme = "https",
        string host = "example.com",
        int? port = null,
        IServiceProvider? services = null,
        Endpoint? endpoint = null)
    {
        var context = new DefaultHttpContext();
        context.RequestServices = services ?? new ServiceCollection().BuildServiceProvider();
        context.Request.Method = method;
        context.Request.Scheme = scheme;

        if (port.HasValue)
        {
            context.Request.Host = new HostString(host, port.Value);
        }
        else
        {
            context.Request.Host = new HostString(host);
        }

        if (secFetchSite is not null)
        {
            context.Request.Headers["Sec-Fetch-Site"] = secFetchSite;
        }

        if (origin is not null)
        {
            context.Request.Headers["Origin"] = origin;
        }

        if (endpoint is not null)
        {
            context.SetEndpoint(endpoint);
        }

        return context;
    }

    private static IServiceProvider BuildCorsServices(Action<CorsOptions> configure)
    {
        var services = new ServiceCollection();
        services.AddCors(configure);
        return services.BuildServiceProvider();
    }

    private static Endpoint EndpointWithMetadata(params object[] metadata)
        => new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "test");

    // Step 1: Safe methods are always allowed regardless of headers.

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    [InlineData("get")]
    [InlineData("Get")]
    public async Task SafeMethods_AlwaysAllowed(string method)
    {
        var context = CreateContext(method: method, secFetchSite: "cross-site", origin: "https://evil.com");
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    // Step 2: Trusted origins (from the applicable CORS policy) allow cross-origin requests.

    [Fact]
    public async Task TrustedOrigin_FromDefaultPolicy_Allowed()
    {
        var services = BuildCorsServices(o => o.AddDefaultPolicy(p => p.WithOrigins("https://trusted.com")));
        var context = CreateContext(origin: "https://trusted.com", secFetchSite: "cross-site", services: services);
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task TrustedOrigin_FromNamedPolicyOnEndpoint_Allowed()
    {
        var services = BuildCorsServices(o => o.AddPolicy("Webhook", p => p.WithOrigins("https://stripe.com")));
        var endpoint = EndpointWithMetadata(new EnableCorsAttribute("Webhook"));
        var context = CreateContext(origin: "https://stripe.com", secFetchSite: "cross-site", services: services, endpoint: endpoint);
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task TrustedOrigin_FromInlinePolicyOnEndpoint_Allowed()
    {
        var policy = new CorsPolicyBuilder().WithOrigins("https://inline.example.com").Build();
        var endpoint = EndpointWithMetadata(new CorsPolicyMetadata(policy));
        var context = CreateContext(origin: "https://inline.example.com", secFetchSite: "cross-site", endpoint: endpoint);
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task InlinePolicyOnEndpoint_Overrides_DefaultPolicy()
    {
        var services = BuildCorsServices(o => o.AddDefaultPolicy(p => p.WithOrigins("https://app.example.com")));
        var inlinePolicy = new CorsPolicyBuilder().WithOrigins("https://inline.example.com").Build();
        var endpoint = EndpointWithMetadata(new CorsPolicyMetadata(inlinePolicy));

        // Default-policy origin would be allowed app-wide, but this endpoint declared a stricter inline policy.
        var context = CreateContext(origin: "https://app.example.com", secFetchSite: "cross-site", services: services, endpoint: endpoint);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NamedPolicyOnEndpoint_Overrides_DefaultPolicy()
    {
        var services = BuildCorsServices(o =>
        {
            o.AddDefaultPolicy(p => p.WithOrigins("https://app.example.com"));
            o.AddPolicy("Webhook", p => p.WithOrigins("https://stripe.com"));
        });
        var endpoint = EndpointWithMetadata(new EnableCorsAttribute("Webhook"));

        // Default-policy origin would be allowed app-wide, but this endpoint declared a stricter named policy.
        var context = CreateContext(origin: "https://app.example.com", secFetchSite: "cross-site", services: services, endpoint: endpoint);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task UnknownNamedPolicyOnEndpoint_FallsThroughToSecFetchSite()
    {
        // Endpoint references a policy name that was never registered → provider returns null → fall through.
        var services = BuildCorsServices(o => o.AddDefaultPolicy(p => p.WithOrigins("https://trusted.com")));
        var endpoint = EndpointWithMetadata(new EnableCorsAttribute("Nonexistent"));
        var context = CreateContext(origin: "https://trusted.com", secFetchSite: "cross-site", services: services, endpoint: endpoint);
        // Even though "https://trusted.com" is in the default policy, the endpoint specified a different named policy
        // which doesn't exist, so no CORS-trust applies and Sec-Fetch denies the cross-site request.
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NoDefaultPolicy_NamedPolicyOnly_UnannotatedEndpoint_FallsThroughToSecFetchSite()
    {
        // AddCors is called and a named policy is registered, but no default policy and no [EnableCors] on endpoint.
        // The provider tries default policy name, finds nothing → returns null → fall through.
        var services = BuildCorsServices(o => o.AddPolicy("Webhook", p => p.WithOrigins("https://stripe.com")));
        var context = CreateContext(origin: "https://stripe.com", secFetchSite: "cross-site", services: services);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task DisableCorsOnEndpoint_SkipsCorsTrust_FallsThroughToSecFetchSite()
    {
        var services = BuildCorsServices(o => o.AddDefaultPolicy(p => p.WithOrigins("https://trusted.com")));
        var endpoint = EndpointWithMetadata(new DisableCorsAttribute());
        var context = CreateContext(origin: "https://trusted.com", secFetchSite: "cross-site", services: services, endpoint: endpoint);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task AllowAnyOriginPolicy_IsIgnored_DoesNotTrustEverything()
    {
        var services = BuildCorsServices(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin()));
        var context = CreateContext(origin: "https://evil.com", secFetchSite: "cross-site", services: services);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NoCorsRegistration_FallsThroughToSecFetchSite()
    {
        var context = CreateContext(origin: "https://untrusted.com", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task UntrustedOrigin_DeniedBySecFetchSite()
    {
        var services = BuildCorsServices(o => o.AddDefaultPolicy(p => p.WithOrigins("https://trusted.com")));
        var context = CreateContext(origin: "https://untrusted.com", secFetchSite: "cross-site", services: services);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    // Step 3: Sec-Fetch-Site header validation.

    [Theory]
    [InlineData("same-origin")]
    [InlineData("none")]
    public async Task SecFetchSite_AllowedValues(string secFetchSite)
    {
        var context = CreateContext(secFetchSite: secFetchSite);
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    [Theory]
    [InlineData("same-site")]
    [InlineData("cross-site")]
    [InlineData("unknown-value")]
    public async Task SecFetchSite_DeniedValues(string secFetchSite)
    {
        var context = CreateContext(secFetchSite: secFetchSite);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    // Step 4: No Sec-Fetch-Site header → fall back to Origin vs Host comparison.

    [Fact]
    public async Task NoSecFetchSite_OriginMatchesHost_Allowed()
    {
        var context = CreateContext(origin: "https://example.com", host: "example.com");
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NoSecFetchSite_OriginDiffersFromHost_Denied()
    {
        var context = CreateContext(origin: "https://other.com", host: "example.com");
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NoSecFetchSite_OriginMatchesHostWithExplicitDefaultPort_Allowed()
    {
        // Host header has no port (default 443); Origin omits port too. Equal → allowed.
        var context = CreateContext(origin: "https://example.com", host: "example.com");
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NoSecFetchSite_OriginAndHostDifferOnPort_Denied()
    {
        var context = CreateContext(origin: "https://example.com:8080", host: "example.com", port: 8443);
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NoSecFetchSite_NoHost_Denied()
    {
        var context = new DefaultHttpContext();
        context.RequestServices = new ServiceCollection().BuildServiceProvider();
        context.Request.Method = "POST";
        context.Request.Scheme = "https";
        context.Request.Headers["Origin"] = "https://example.com";
        // No Host header set → Host.HasValue is false → cannot determine request origin
        Assert.Equal(CsrfProtectionResult.Denied, await _validator.ValidateAsync(context));
    }

    // Step 5: No Sec-Fetch-Site AND no Origin → non-browser → allowed.

    [Fact]
    public async Task NoHeaders_NonBrowserClient_Allowed()
    {
        var context = CreateContext();
        Assert.Equal(CsrfProtectionResult.Allowed, await _validator.ValidateAsync(context));
    }

    [Fact]
    public async Task NullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await _validator.ValidateAsync(null!));
    }
}
