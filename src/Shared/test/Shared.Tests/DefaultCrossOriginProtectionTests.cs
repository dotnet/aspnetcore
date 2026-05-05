// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

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
        int? port = null)
    {
        var context = new DefaultHttpContext();
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

        return context;
    }

    // Step 1: Safe methods are always allowed regardless of headers.

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("OPTIONS")]
    [InlineData("TRACE")]
    [InlineData("get")]
    [InlineData("Get")]
    public void SafeMethods_AlwaysAllowed(string method)
    {
        var context = CreateContext(method: method, secFetchSite: "cross-site", origin: "https://evil.com");
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    // Step 2: Trusted origins allow cross-origin requests.

    [Fact]
    public void TrustedOrigin_Allowed()
    {
        var validator = new DefaultCrossOriginProtection(["https://trusted.com"]);
        var context = CreateContext(origin: "https://trusted.com", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Allowed, validator.Validate(context));
    }

    [Fact]
    public void TrustedOrigin_CaseInsensitive()
    {
        var validator = new DefaultCrossOriginProtection(["https://Trusted.COM"]);
        var context = CreateContext(origin: "https://trusted.com", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Allowed, validator.Validate(context));
    }

    [Fact]
    public void TrustedOrigin_WithPort()
    {
        var validator = new DefaultCrossOriginProtection(["https://trusted.com:8443"]);
        var context = CreateContext(origin: "https://trusted.com:8443", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Allowed, validator.Validate(context));
    }

    [Fact]
    public void TrustedOrigin_DefaultPortNormalized()
    {
        var validator = new DefaultCrossOriginProtection(["https://trusted.com:443"]);
        var context = CreateContext(origin: "https://trusted.com", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Allowed, validator.Validate(context));
    }

    [Fact]
    public void UntrustedOrigin_DeniedBySecFetchSite()
    {
        var validator = new DefaultCrossOriginProtection(["https://trusted.com"]);
        var context = CreateContext(origin: "https://untrusted.com", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Denied, validator.Validate(context));
    }

    // Step 3: Sec-Fetch-Site header validation.

    [Theory]
    [InlineData("same-origin")]
    [InlineData("none")]
    public void SecFetchSite_SafeValues_Allowed(string value)
    {
        var context = CreateContext(secFetchSite: value);
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    [Theory]
    [InlineData("cross-site")]
    [InlineData("same-site")]
    public void SecFetchSite_UnsafeValues_Denied(string value)
    {
        var context = CreateContext(secFetchSite: value);
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    [Fact]
    public void SecFetchSite_UnknownValue_Denied()
    {
        var context = CreateContext(secFetchSite: "something-unexpected");
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    // Step 4: No Sec-Fetch-Site — fall back to Origin vs Host comparison.

    [Fact]
    public void NoSecFetchSite_OriginMatchesHost_Allowed()
    {
        var context = CreateContext(origin: "https://example.com", secFetchSite: null);
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    [Fact]
    public void NoSecFetchSite_OriginMatchesHostWithPort_Allowed()
    {
        var context = CreateContext(origin: "https://example.com:8443", secFetchSite: null, port: 8443);
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    [Fact]
    public void NoSecFetchSite_OriginDoesNotMatchHost_Denied()
    {
        var context = CreateContext(origin: "https://evil.com", secFetchSite: null);
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    [Fact]
    public void NoSecFetchSite_OriginDifferentScheme_Denied()
    {
        var context = CreateContext(origin: "http://example.com", secFetchSite: null, scheme: "https");
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    [Fact]
    public void NoSecFetchSite_OriginDifferentPort_Denied()
    {
        var context = CreateContext(origin: "https://example.com:9999", secFetchSite: null, port: 8443);
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    [Fact]
    public void NoSecFetchSite_OriginDefaultPort_MatchesHostWithoutPort_Allowed()
    {
        // Origin "https://example.com:443" should normalize to "https://example.com"
        var context = CreateContext(origin: "https://example.com:443", secFetchSite: null);
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    [Fact]
    public void NoSecFetchSite_MalformedOrigin_Denied()
    {
        var context = CreateContext(origin: "not-a-valid-uri", secFetchSite: null);
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    [Fact]
    public void NoSecFetchSite_NullOriginValue_Denied()
    {
        // Origin: "null" is sent by privacy-sensitive contexts (e.g., sandboxed iframes, redirects).
        // It fails URI normalization (no valid scheme/host) → treated as malformed origin → denied.
        var context = CreateContext(secFetchSite: null);
        context.Request.Headers["Origin"] = "null";
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    // Step 5: No Sec-Fetch-Site AND no Origin → non-browser client → allow.

    [Fact]
    public void NoHeaders_NonBrowserClient_Allowed()
    {
        var context = CreateContext(secFetchSite: null, origin: null);
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    [InlineData("PATCH")]
    public void NoHeaders_UnsafeMethods_NonBrowserClient_Allowed(string method)
    {
        var context = CreateContext(method: method, secFetchSite: null, origin: null);
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    // Sec-Fetch-Site takes precedence over Origin match.

    [Fact]
    public void SecFetchSite_CrossSite_DeniesEvenWithMatchingOrigin()
    {
        var context = CreateContext(secFetchSite: "cross-site", origin: "https://example.com");
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    [Fact]
    public void SecFetchSite_SameOrigin_AllowsEvenWithMismatchedOriginHeader()
    {
        // Sec-Fetch-Site is authoritative; browser sets it.
        var context = CreateContext(secFetchSite: "same-origin", origin: "https://evil.com");
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    // Edge cases.

    [Fact]
    public void EmptyOriginHeader_TreatedAsAbsent()
    {
        var context = CreateContext(secFetchSite: null);
        context.Request.Headers["Origin"] = "";
        Assert.Equal(CsrfProtectionResult.Allowed, _validator.Validate(context));
    }

    [Fact]
    public void NoHost_OriginPresent_Denied()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Scheme = "https";
        context.Request.Headers["Origin"] = "https://example.com";
        // No Host header set → Host.HasValue is false → cannot determine request origin
        Assert.Equal(CsrfProtectionResult.Denied, _validator.Validate(context));
    }

    [Fact]
    public void NullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _validator.Validate(null!));
    }

    // TrustedOrigins normalization.

    [Fact]
    public void TrustedOrigins_MalformedEntriesIgnored()
    {
        var validator = new DefaultCrossOriginProtection(["not-valid", "https://good.com", ""]);
        var context = CreateContext(origin: "https://good.com", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Allowed, validator.Validate(context));
    }

    [Fact]
    public void TrustedOrigins_WithPathRejected()
    {
        // Origins should not have paths
        var validator = new DefaultCrossOriginProtection(["https://example.com/path"]);
        var context = CreateContext(origin: "https://example.com", secFetchSite: "cross-site");
        Assert.Equal(CsrfProtectionResult.Denied, validator.Validate(context));
    }

    [Fact]
    public void TrustedOrigins_Empty_NoTrustedOriginMatching()
    {
        var validator = new DefaultCrossOriginProtection([]);
        var context = CreateContext(origin: "https://example.com", secFetchSite: "same-origin");
        Assert.Equal(CsrfProtectionResult.Allowed, validator.Validate(context));
    }

    [Fact]
    public void HttpDefaultPort80_Normalized()
    {
        var validator = new DefaultCrossOriginProtection(["http://trusted.com:80"]);
        var context = CreateContext(origin: "http://trusted.com", secFetchSite: "cross-site", scheme: "http");
        Assert.Equal(CsrfProtectionResult.Allowed, validator.Validate(context));
    }
}
