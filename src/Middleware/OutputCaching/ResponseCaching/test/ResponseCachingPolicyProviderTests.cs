// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.NewResponseCaching.Tests;

public class ResponseCachingPolicyProviderTests
{
    public static TheoryData<string> CacheableMethods
    {
        get
        {
            return new TheoryData<string>
                {
                    HttpMethods.Get,
                    HttpMethods.Head
                };
        }
    }

    [Theory]
    [MemberData(nameof(CacheableMethods))]
    public void AttemptResponseCaching_CacheableMethods_Allowed(string method)
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = method;

        Assert.True(new ResponseCachingPolicyProvider().AttemptResponseCaching(context));
    }
    public static TheoryData<string> NonCacheableMethods
    {
        get
        {
            return new TheoryData<string>
                {
                    HttpMethods.Post,
                    HttpMethods.Put,
                    HttpMethods.Delete,
                    HttpMethods.Trace,
                    HttpMethods.Connect,
                    HttpMethods.Options,
                    "",
                    null
                };
        }
    }

    [Theory]
    [MemberData(nameof(NonCacheableMethods))]
    public void AttemptResponseCaching_UncacheableMethods_NotAllowed(string method)
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = method;

        Assert.False(new ResponseCachingPolicyProvider().AttemptResponseCaching(context));
    }

    [Fact]
    public void AttemptResponseCaching_AuthorizationHeaders_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Headers.Authorization = "Placeholder";

        Assert.False(new ResponseCachingPolicyProvider().AttemptResponseCaching(context));
    }

    [Fact]
    public void AllowCacheStorage_NoStore_Allowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            NoStore = true
        }.ToString();

        Assert.True(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
    }

    [Fact]
    public void AllowCacheLookup_NoCache_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            NoCache = true
        }.ToString();

        Assert.False(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
    }

    [Fact]
    public void AllowCacheLookup_LegacyDirectives_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Headers.Pragma = "no-cache";

        Assert.False(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
    }

    [Fact]
    public void AllowCacheLookup_LegacyDirectives_OverridenByCacheControl()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Headers.Pragma = "no-cache";
        context.HttpContext.Request.Headers.CacheControl = "max-age=10";

        Assert.True(new ResponseCachingPolicyProvider().AllowCacheLookup(context));
    }

    [Fact]
    public void AllowCacheStorage_NoStore_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Method = HttpMethods.Get;
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            NoStore = true
        }.ToString();

        Assert.False(new ResponseCachingPolicyProvider().AllowCacheStorage(context));
    }

    [Fact]
    public void IsResponseCacheable_NoPublic_NotAllowed()
    {
        var context = new PolicyTestContext();

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_Public_Allowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();

        Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_NoCache_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            NoCache = true
        }.ToString();

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_ResponseNoStore_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            NoStore = true
        }.ToString();

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_SetCookieHeader_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();
        context.HttpContext.Response.Headers.SetCookie = "cookieName=cookieValue";

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    public static TheoryData<StringValues> VaryHeaderWithStarData
    {
        get
        {
            return new TheoryData<StringValues>
            {
                "*",
                new StringValues(["*", "Accept-Encoding"]),
                "*, Accept-Encoding",
                "Accept-Encoding, *",
                "gzip, *, br"
            };
        }
    }

    [Theory]
    [MemberData(nameof(VaryHeaderWithStarData))]
    public void IsResponseCacheable_VaryHeaderByStar_NotAllowed(StringValues vary)
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();
        context.HttpContext.Response.Headers.Vary = vary;

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_ValidVaryHeaderWithoutAsterisk_Allowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();
        context.HttpContext.Response.Headers.Vary = "Accept-Encoding, User-Agent";

        Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_Private_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            Private = true
        }.ToString();

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Theory]
    [InlineData(StatusCodes.Status200OK)]
    public void IsResponseCacheable_SuccessStatusCodes_Allowed(int statusCode)
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = statusCode;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();

        Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Theory]
    [InlineData(StatusCodes.Status100Continue)]
    [InlineData(StatusCodes.Status101SwitchingProtocols)]
    [InlineData(StatusCodes.Status102Processing)]
    [InlineData(StatusCodes.Status201Created)]
    [InlineData(StatusCodes.Status202Accepted)]
    [InlineData(StatusCodes.Status203NonAuthoritative)]
    [InlineData(StatusCodes.Status204NoContent)]
    [InlineData(StatusCodes.Status205ResetContent)]
    [InlineData(StatusCodes.Status206PartialContent)]
    [InlineData(StatusCodes.Status207MultiStatus)]
    [InlineData(StatusCodes.Status208AlreadyReported)]
    [InlineData(StatusCodes.Status226IMUsed)]
    [InlineData(StatusCodes.Status300MultipleChoices)]
    [InlineData(StatusCodes.Status301MovedPermanently)]
    [InlineData(StatusCodes.Status302Found)]
    [InlineData(StatusCodes.Status303SeeOther)]
    [InlineData(StatusCodes.Status304NotModified)]
    [InlineData(StatusCodes.Status305UseProxy)]
    [InlineData(StatusCodes.Status306SwitchProxy)]
    [InlineData(StatusCodes.Status307TemporaryRedirect)]
    [InlineData(StatusCodes.Status308PermanentRedirect)]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status401Unauthorized)]
    [InlineData(StatusCodes.Status402PaymentRequired)]
    [InlineData(StatusCodes.Status403Forbidden)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status405MethodNotAllowed)]
    [InlineData(StatusCodes.Status406NotAcceptable)]
    [InlineData(StatusCodes.Status407ProxyAuthenticationRequired)]
    [InlineData(StatusCodes.Status408RequestTimeout)]
    [InlineData(StatusCodes.Status409Conflict)]
    [InlineData(StatusCodes.Status410Gone)]
    [InlineData(StatusCodes.Status411LengthRequired)]
    [InlineData(StatusCodes.Status412PreconditionFailed)]
    [InlineData(StatusCodes.Status413RequestEntityTooLarge)]
    [InlineData(StatusCodes.Status414RequestUriTooLong)]
    [InlineData(StatusCodes.Status415UnsupportedMediaType)]
    [InlineData(StatusCodes.Status416RequestedRangeNotSatisfiable)]
    [InlineData(StatusCodes.Status417ExpectationFailed)]
    [InlineData(StatusCodes.Status418ImATeapot)]
    [InlineData(StatusCodes.Status419AuthenticationTimeout)]
    [InlineData(StatusCodes.Status421MisdirectedRequest)]
    [InlineData(StatusCodes.Status422UnprocessableEntity)]
    [InlineData(StatusCodes.Status423Locked)]
    [InlineData(StatusCodes.Status424FailedDependency)]
    [InlineData(StatusCodes.Status426UpgradeRequired)]
    [InlineData(StatusCodes.Status428PreconditionRequired)]
    [InlineData(StatusCodes.Status429TooManyRequests)]
    [InlineData(StatusCodes.Status431RequestHeaderFieldsTooLarge)]
    [InlineData(StatusCodes.Status451UnavailableForLegalReasons)]
    [InlineData(StatusCodes.Status500InternalServerError)]
    [InlineData(StatusCodes.Status501NotImplemented)]
    [InlineData(StatusCodes.Status502BadGateway)]
    [InlineData(StatusCodes.Status503ServiceUnavailable)]
    [InlineData(StatusCodes.Status504GatewayTimeout)]
    [InlineData(StatusCodes.Status505HttpVersionNotsupported)]
    [InlineData(StatusCodes.Status506VariantAlsoNegotiates)]
    [InlineData(StatusCodes.Status507InsufficientStorage)]
    [InlineData(StatusCodes.Status508LoopDetected)]
    [InlineData(StatusCodes.Status510NotExtended)]
    [InlineData(StatusCodes.Status511NetworkAuthenticationRequired)]
    public void IsResponseCacheable_NonSuccessStatusCodes_NotAllowed(int statusCode)
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = statusCode;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_NoExpiryRequirements_IsAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();

        var utcNow = DateTimeOffset.UtcNow;
        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = DateTimeOffset.MaxValue;

        Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_AtExpiry_NotAllowed()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();
        var utcNow = DateTimeOffset.UtcNow;
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow);

        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = utcNow;

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_MaxAgeOverridesExpiry_ToAllowed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10)
        }.ToString();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow);
        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = utcNow + TimeSpan.FromSeconds(9);

        Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_MaxAgeOverridesExpiry_ToNotAllowed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10)
        }.ToString();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow);
        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = utcNow + TimeSpan.FromSeconds(10);

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_SharedMaxAgeOverridesMaxAge_ToAllowed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10),
            SharedMaxAge = TimeSpan.FromSeconds(15)
        }.ToString();
        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = utcNow + TimeSpan.FromSeconds(11);

        Assert.True(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsResponseCacheable_SharedMaxAgeOverridesMaxAge_ToNotAllowed()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.Headers.CacheControl = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10),
            SharedMaxAge = TimeSpan.FromSeconds(5)
        }.ToString();
        context.HttpContext.Response.Headers.Date = HeaderUtilities.FormatDate(utcNow);
        context.ResponseTime = utcNow + TimeSpan.FromSeconds(5);

        Assert.False(new ResponseCachingPolicyProvider().IsResponseCacheable(context));
    }

    [Fact]
    public void IsCachedEntryFresh_NoCachedCacheControl_FallsbackToEmptyCacheControl()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.ResponseTime = DateTimeOffset.MaxValue;
        context.CachedEntryAge = TimeSpan.MaxValue;
        context.CachedResponseHeaders = new HeaderDictionary();

        Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_NoExpiryRequirements_IsFresh()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.ResponseTime = DateTimeOffset.MaxValue;
        context.CachedEntryAge = TimeSpan.MaxValue;
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();

        Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_AtExpiry_IsNotFresh()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.ResponseTime = utcNow;
        context.CachedEntryAge = TimeSpan.Zero;
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            Public = true
        }.ToString();
        context.CachedResponseHeaders[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_MaxAgeOverridesExpiry_ToFresh()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.CachedEntryAge = TimeSpan.FromSeconds(9);
        context.ResponseTime = utcNow + context.CachedEntryAge;
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10)
        }.ToString();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow);

        Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_MaxAgeOverridesExpiry_ToNotFresh()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.CachedEntryAge = TimeSpan.FromSeconds(10);
        context.ResponseTime = utcNow + context.CachedEntryAge;
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10)
        }.ToString();
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_SharedMaxAgeOverridesMaxAge_ToFresh()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.CachedEntryAge = TimeSpan.FromSeconds(11);
        context.ResponseTime = utcNow + context.CachedEntryAge;
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10),
            SharedMaxAge = TimeSpan.FromSeconds(15)
        }.ToString();
        context.CachedResponseHeaders[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

        Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_SharedMaxAgeOverridesMaxAge_ToNotFresh()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var context = new PolicyTestContext();
        context.CachedEntryAge = TimeSpan.FromSeconds(5);
        context.ResponseTime = utcNow + context.CachedEntryAge;
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(10),
            SharedMaxAge = TimeSpan.FromSeconds(5)
        }.ToString();
        context.CachedResponseHeaders[HeaderNames.Expires] = HeaderUtilities.FormatDate(utcNow);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_MinFreshReducesFreshness_ToNotFresh()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MinFresh = TimeSpan.FromSeconds(2)
        }.ToString();
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(10),
            SharedMaxAge = TimeSpan.FromSeconds(5)
        }.ToString();
        context.CachedEntryAge = TimeSpan.FromSeconds(3);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_RequestMaxAgeRestrictAge_ToNotFresh()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5)
        }.ToString();
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(10),
        }.ToString();
        context.CachedEntryAge = TimeSpan.FromSeconds(5);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_MaxStaleOverridesFreshness_ToFresh()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
            MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
            MaxStaleLimit = TimeSpan.FromSeconds(2)
        }.ToString();
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
        }.ToString();
        context.CachedEntryAge = TimeSpan.FromSeconds(6);

        Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_MaxStaleInfiniteOverridesFreshness_ToFresh()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
            MaxStale = true // No value specified means a MaxStaleLimit of infinity
        }.ToString();
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
        }.ToString();
        context.CachedEntryAge = TimeSpan.FromSeconds(6);

        Assert.True(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_MaxStaleOverridesFreshness_ButStillNotFresh()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
            MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
            MaxStaleLimit = TimeSpan.FromSeconds(1)
        }.ToString();
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
        }.ToString();
        context.CachedEntryAge = TimeSpan.FromSeconds(6);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_MustRevalidateOverridesRequestMaxStale_ToNotFresh()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
            MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
            MaxStaleLimit = TimeSpan.FromSeconds(2)
        }.ToString();
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
            MustRevalidate = true
        }.ToString();
        context.CachedEntryAge = TimeSpan.FromSeconds(6);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public void IsCachedEntryFresh_ProxyRevalidateOverridesRequestMaxStale_ToNotFresh()
    {
        var context = new PolicyTestContext();
        context.HttpContext.Request.Headers.CacheControl = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
            MaxStale = true, // This value must be set to true in order to specify MaxStaleLimit
            MaxStaleLimit = TimeSpan.FromSeconds(2)
        }.ToString();
        context.CachedResponseHeaders = new HeaderDictionary();
        context.CachedResponseHeaders[HeaderNames.CacheControl] = new CacheControlHeaderValue()
        {
            MaxAge = TimeSpan.FromSeconds(5),
            MustRevalidate = true
        }.ToString();
        context.CachedEntryAge = TimeSpan.FromSeconds(6);

        Assert.False(new ResponseCachingPolicyProvider().IsCachedEntryFresh(context));
    }

    [Fact]
    public async Task ResponseExpiration_DefaultIs10Seconds()
    {
        var context = CreateCacheableResponseContext(DateTimeOffset.UtcNow, new CacheControlHeaderValue { Public = true });

        await ((IOutputCachePolicy)ResponseCachingPolicy.Instance).ServeResponseAsync(context, default);

        Assert.True(context.AllowCacheStorage);
        Assert.Equal(TimeSpan.FromSeconds(10), context.ResponseExpirationTimeSpan);
    }

    [Fact]
    public async Task ResponseExpiration_UsesExpiryIfAvailable()
    {
        // HTTP dates have a precision of one second.
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var context = CreateCacheableResponseContext(utcNow, new CacheControlHeaderValue { Public = true });
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(11));

        await ((IOutputCachePolicy)ResponseCachingPolicy.Instance).ServeResponseAsync(context, default);

        Assert.True(context.AllowCacheStorage);
        Assert.Equal(TimeSpan.FromSeconds(11), context.ResponseExpirationTimeSpan);
    }

    [Fact]
    public async Task ResponseExpiration_MaxAgeOverridesExpiry()
    {
        // HTTP dates have a precision of one second.
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var context = CreateCacheableResponseContext(utcNow, new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromSeconds(12) });
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(11));

        await ((IOutputCachePolicy)ResponseCachingPolicy.Instance).ServeResponseAsync(context, default);

        Assert.True(context.AllowCacheStorage);
        Assert.Equal(TimeSpan.FromSeconds(12), context.ResponseExpirationTimeSpan);
    }

    [Fact]
    public async Task ResponseExpiration_SharedMaxAgeOverridesMaxAge()
    {
        // HTTP dates have a precision of one second.
        var utcNow = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var context = CreateCacheableResponseContext(utcNow, new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromSeconds(12), SharedMaxAge = TimeSpan.FromSeconds(13) });
        context.HttpContext.Response.Headers.Expires = HeaderUtilities.FormatDate(utcNow + TimeSpan.FromSeconds(11));

        await ((IOutputCachePolicy)ResponseCachingPolicy.Instance).ServeResponseAsync(context, default);

        Assert.True(context.AllowCacheStorage);
        Assert.Equal(TimeSpan.FromSeconds(13), context.ResponseExpirationTimeSpan);
    }

    [Theory]
    [InlineData("From", "From", true)]
    [InlineData("from", "From", true)]
    [InlineData("From, Accept-Language", "From", false)]
    [InlineData("From", null, false)]
    [InlineData("*", "From", false)]
    public async Task ResponseWithVaryHeader_IsStoredOnlyIfDeclared(string vary, string declaredHeader, bool isStored)
    {
        var context = CreateCacheableResponseContext(DateTimeOffset.UtcNow, new CacheControlHeaderValue { Public = true, MaxAge = TimeSpan.FromSeconds(10) });
        context.HttpContext.Response.Headers.Vary = vary;
        context.CacheVaryByRules.HeaderNames = declaredHeader;

        await ((IOutputCachePolicy)ResponseCachingPolicy.Instance).ServeResponseAsync(context, default);

        Assert.Equal(isStored, context.AllowCacheStorage);
    }

    private static OutputCacheContext CreateCacheableResponseContext(DateTimeOffset responseTime, CacheControlHeaderValue cacheControl)
    {
        var context = new OutputCacheContext
        {
            HttpContext = new DefaultHttpContext(),
            ResponseTime = responseTime,
            AllowCacheStorage = true
        };
        context.HttpContext.Response.Headers.CacheControl = cacheControl.ToString();

        return context;
    }

    // Mirrors the shape of the Response Caching context so the original test bodies can be reused against ResponseCachingPolicy.
    private sealed class PolicyTestContext
    {
        public HttpContext HttpContext { get; } = new DefaultHttpContext();

        public DateTimeOffset? ResponseTime { get; set; } = DateTimeOffset.UtcNow;

        public TimeSpan? CachedEntryAge { get; set; }

        public IHeaderDictionary CachedResponseHeaders { get; set; } = new HeaderDictionary();

        public OutputCacheContext ToOutputCacheContext()
        {
            var context = new OutputCacheContext
            {
                HttpContext = HttpContext,
                ResponseTime = ResponseTime,
                CachedEntryAge = CachedEntryAge ?? TimeSpan.Zero,
                CachedResponse = new OutputCacheEntry(DateTimeOffset.UtcNow, StatusCodes.Status200OK).CopyHeadersFrom(CachedResponseHeaders)
            };

            return context;
        }
    }

    private sealed class ResponseCachingPolicyProvider
    {
        public bool AttemptResponseCaching(PolicyTestContext context) => ResponseCachingPolicy.AttemptResponseCaching(context.ToOutputCacheContext());

        public bool AllowCacheLookup(PolicyTestContext context) => ResponseCachingPolicy.AllowCacheLookup(context.ToOutputCacheContext());

        public bool AllowCacheStorage(PolicyTestContext context) => ResponseCachingPolicy.AllowCacheStorage(context.ToOutputCacheContext());

        public bool IsResponseCacheable(PolicyTestContext context) => ResponseCachingPolicy.IsResponseCacheable(context.ToOutputCacheContext(), context.ResponseTime!.Value, out _, out _, out _);

        public bool IsCachedEntryFresh(PolicyTestContext context) => ResponseCachingPolicy.IsCachedEntryFresh(context.ToOutputCacheContext());
    }
}
