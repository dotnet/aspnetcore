// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Http.Tests;

public class ResponseCookiesTest
{
    private IFeatureCollection MakeFeatures(IHeaderDictionary headers)
    {
        var responseFeature = new HttpResponseFeature()
        {
            Headers = headers
        };
        var features = new FeatureCollection();
        features.Set<IHttpResponseFeature>(responseFeature);
        return features;
    }

    [Fact]
    public void AppendSameSiteNoneWithoutSecureLogsWarning()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var services = new ServiceCollection();

        var sink = new TestSink(TestSink.EnableWithTypeName<ResponseCookies>);
        var loggerFactory = new TestLoggerFactory(sink, enabled: true);
        services.AddLogging();
        services.AddSingleton<ILoggerFactory>(loggerFactory);

        features.Set<IServiceProvidersFeature>(new ServiceProvidersFeature() { RequestServices = services.BuildServiceProvider() });

        var cookies = new ResponseCookies(features);
        var testCookie = "TestCookie";

        cookies.Append(testCookie, "value", new CookieOptions()
        {
            SameSite = SameSiteMode.None,
        });

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.StartsWith(testCookie, cookieHeaderValues[0]);
        Assert.Contains("path=/", cookieHeaderValues[0]);
        Assert.Contains("samesite=none", cookieHeaderValues[0]);
        Assert.DoesNotContain("secure", cookieHeaderValues[0]);

        var writeContext = Assert.Single(sink.Writes);
        Assert.Equal("The cookie 'TestCookie' has set 'SameSite=None' and must also set 'Secure'.", writeContext.Message);
    }

    [Fact]
    public void AppendWithExtensions()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);
        var testCookie = "TestCookie";

        cookies.Append(testCookie, "value", new CookieOptions()
        {
            Extensions = { "simple", "key=value" }
        });

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.StartsWith(testCookie, cookieHeaderValues[0]);
        Assert.Contains("path=/", cookieHeaderValues[0]);
        Assert.Contains("simple;", cookieHeaderValues[0]);
        Assert.EndsWith("key=value", cookieHeaderValues[0]);
    }

    [Fact]
    public void DeleteWithExtensions()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);
        var testCookie = "TestCookie";

        cookies.Delete(testCookie, new CookieOptions()
        {
            Extensions = { "simple", "key=value" }
        });

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.StartsWith(testCookie, cookieHeaderValues[0]);
        Assert.Contains("path=/", cookieHeaderValues[0]);
        Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        Assert.Contains("simple;", cookieHeaderValues[0]);
        Assert.EndsWith("key=value", cookieHeaderValues[0]);
    }

    [Fact]
    public void DeleteCookieShouldSetDefaultPath()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);
        var testCookie = "TestCookie";

        cookies.Delete(testCookie);

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.StartsWith(testCookie, cookieHeaderValues[0]);
        Assert.Contains("path=/", cookieHeaderValues[0]);
        Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
    }

    [Fact]
    public void DeleteCookieWithDomainAndPathDeletesPriorMatchingCookies()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var responseCookies = new ResponseCookies(features);

        var testCookies = new (string Key, string Path, string Domain)[]
        {
                new ("key1", "/path1/", null),
                new ("key1", "/path2/", null),
                new ("key2", "/path1/", "localhost"),
                new ("key2", "/path2/", "localhost"),
        };

        foreach (var cookie in testCookies)
        {
            responseCookies.Delete(cookie.Key, new CookieOptions() { Domain = cookie.Domain, Path = cookie.Path });
        }

        var deletedCookies = headers.SetCookie.ToArray();
        Assert.Equal(testCookies.Length, deletedCookies.Length);

        Assert.Single(deletedCookies, cookie => cookie.StartsWith("key1", StringComparison.InvariantCulture) && cookie.Contains("path=/path1/"));
        Assert.Single(deletedCookies, cookie => cookie.StartsWith("key1", StringComparison.InvariantCulture) && cookie.Contains("path=/path2/"));
        Assert.Single(deletedCookies, cookie => cookie.StartsWith("key2", StringComparison.InvariantCulture) && cookie.Contains("path=/path1/") && cookie.Contains("domain=localhost"));
        Assert.Single(deletedCookies, cookie => cookie.StartsWith("key2", StringComparison.InvariantCulture) && cookie.Contains("path=/path2/") && cookie.Contains("domain=localhost"));
        Assert.All(deletedCookies, cookie => Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookie));
    }

    [Fact]
    public void DeleteRemovesCookieWithDomainAndPathCreatedByAdd()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var responseCookies = new ResponseCookies(features);

        var testCookies = new (string Key, string Path, string Domain)[]
        {
                new ("key1", "/path1/", null),
                new ("key1", "/path1/", null),
                new ("key2", "/path1/", "localhost"),
                new ("key2", "/path1/", "localhost"),
        };

        foreach (var cookie in testCookies)
        {
            responseCookies.Append(cookie.Key, cookie.Key, new CookieOptions() { Domain = cookie.Domain, Path = cookie.Path });
            responseCookies.Delete(cookie.Key, new CookieOptions() { Domain = cookie.Domain, Path = cookie.Path });
        }

        var deletedCookies = headers.SetCookie.ToArray();
        Assert.Equal(2, deletedCookies.Length);
        Assert.Single(deletedCookies, cookie => cookie.StartsWith("key1", StringComparison.InvariantCulture) && cookie.Contains("path=/path1/"));
        Assert.Single(deletedCookies, cookie => cookie.StartsWith("key2", StringComparison.InvariantCulture) && cookie.Contains("path=/path1/") && cookie.Contains("domain=localhost"));
        Assert.All(deletedCookies, cookie => Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookie));
    }

    [Fact]
    public void DeleteCookieWithCookieOptionsShouldKeepPropertiesOfCookieOptions()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);
        var testCookie = "TestCookie";
        var time = new DateTimeOffset(2000, 1, 1, 1, 1, 1, 1, TimeSpan.Zero);
        var options = new CookieOptions
        {
            Secure = true,
            HttpOnly = true,
            Path = "/",
            Expires = time,
            Domain = "example.com",
            SameSite = SameSiteMode.Lax,
            Extensions = { "extension" }
        };

        cookies.Delete(testCookie, options);

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.StartsWith(testCookie, cookieHeaderValues[0]);
        Assert.Contains("path=/", cookieHeaderValues[0]);
        Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
        Assert.Contains("secure", cookieHeaderValues[0]);
        Assert.Contains("httponly", cookieHeaderValues[0]);
        Assert.Contains("samesite", cookieHeaderValues[0]);
        Assert.Contains("extension", cookieHeaderValues[0]);
    }

    [Fact]
    public void NoParamsDeleteRemovesCookieCreatedByAdd()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);
        var testCookie = "TestCookie";

        cookies.Append(testCookie, testCookie);
        cookies.Delete(testCookie);

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.StartsWith(testCookie, cookieHeaderValues[0]);
        Assert.Contains("path=/", cookieHeaderValues[0]);
        Assert.Contains("expires=Thu, 01 Jan 1970 00:00:00 GMT", cookieHeaderValues[0]);
    }

    [Fact]
    public void ProvidesMaxAgeWithCookieOptionsArgumentExpectMaxAgeToBeSet()
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);
        var cookieOptions = new CookieOptions();
        var maxAgeTime = TimeSpan.FromHours(1);
        cookieOptions.MaxAge = TimeSpan.FromHours(1);
        var testCookie = "TestCookie";

        cookies.Append(testCookie, testCookie, cookieOptions);

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.Contains($"max-age={maxAgeTime.TotalSeconds}", cookieHeaderValues[0]);
    }

    [Theory]
    [InlineData("value", "key=value")]
    [InlineData("!value", "key=%21value")]
    [InlineData("val^ue", "key=val%5Eue")]
    [InlineData("QUI+REU/Rw==", "key=QUI%2BREU%2FRw%3D%3D")]
    public void EscapesValuesBeforeSettingCookie(string value, string expected)
    {
        var headers = (IHeaderDictionary)new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);

        cookies.Append("key", value);

        var cookieHeaderValues = headers.SetCookie;
        Assert.Single(cookieHeaderValues);
        Assert.StartsWith(expected, cookieHeaderValues[0]);
    }

    [Theory]
    [InlineData("key,")]
    [InlineData("ke@y")]
    public void InvalidKeysThrow(string key)
    {
        var headers = new HeaderDictionary();
        var features = MakeFeatures(headers);
        var cookies = new ResponseCookies(features);

        Assert.Throws<ArgumentException>(() => cookies.Append(key, "1"));
    }
}
