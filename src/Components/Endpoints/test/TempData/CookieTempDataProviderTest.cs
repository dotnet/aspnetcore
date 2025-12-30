// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Endpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CookieTempDataProviderTest
{
    private readonly CookieTempDataProvider cookieTempDataProvider;

    public CookieTempDataProviderTest()
    {
        cookieTempDataProvider = new CookieTempDataProvider(
            new EphemeralDataProtectionProvider(),
            Options.Create<CookieTempDataProviderOptions>(new()),
            new JsonTempDataSerializer(),
            NullLogger<CookieTempDataProvider>.Instance);
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_WhenNoCookieExists()
    {
        var httpContext = CreateHttpContext();
        var tempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
    }

    [Fact]
    public void Save_DeletesCookie_WhenNoDataToSave()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());

        var cookieFeature = httpContext.Features.Get<TestResponseCookiesFeature>();
        Assert.NotNull(cookieFeature);
        Assert.Contains(".AspNetCore.Components.TempData", cookieFeature.DeletedCookies);
    }

    [Fact]
    public void Save_SetsCookie_WhenDataExists()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        tempData["Key1"] = "Value1";

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());

        var cookieFeature = httpContext.Features.Get<TestResponseCookiesFeature>();
        Assert.NotNull(cookieFeature);
        Assert.True(cookieFeature.SetCookies.ContainsKey(".AspNetCore.Components.TempData"));
    }

    [Fact]
    public void RoundTrip_PreservesStringValue()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        tempData["StringKey"] = "StringValue";

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal("StringValue", loadedTempData["StringKey"]);
    }

    [Fact]
    public void RoundTrip_PreservesIntValue()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        tempData["IntKey"] = 42;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal(42, loadedTempData["IntKey"]);
    }

    [Fact]
    public void RoundTrip_PreservesBoolValue()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        tempData["BoolKey"] = true;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal(true, loadedTempData["BoolKey"]);
    }

    [Fact]
    public void RoundTrip_PreservesGuidValue()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        var guid = Guid.NewGuid();
        tempData["GuidKey"] = guid;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal(guid, loadedTempData["GuidKey"]);
    }

    [Fact]
    public void RoundTrip_PreservesDateTimeValue()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        var dateTime = new DateTime(2025, 12, 15, 10, 30, 0, DateTimeKind.Utc);
        tempData["DateTimeKey"] = dateTime;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal(dateTime, loadedTempData["DateTimeKey"]);
    }

    [Fact]
    public void RoundTrip_PreservesStringArray()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        var array = new[] { "one", "two", "three" };
        tempData["ArrayKey"] = array;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal(array, loadedTempData["ArrayKey"]);
    }

    [Fact]
    public void RoundTrip_PreservesIntArray()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        var array = new[] { 1, 2, 3 };
        tempData["ArrayKey"] = array;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal(array, loadedTempData["ArrayKey"]);
    }

    [Fact]
    public void RoundTrip_PreservesDictionary()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        var dict = new Dictionary<string, string> { ["a"] = "1", ["b"] = "2" };
        tempData["DictKey"] = dict;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        var loadedDict = Assert.IsType<Dictionary<string, object>>(loadedTempData["DictKey"]);
        Assert.Equal("1", loadedDict["a"]);
        Assert.Equal("2", loadedDict["b"]);
    }

    [Fact]
    public void RoundTrip_PreservesMultipleDifferentValues()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        tempData["Key1"] = "Value1";
        tempData["Key2"] = 123;
        tempData["Key3"] = true;

        cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal("Value1", loadedTempData["Key1"]);
        Assert.Equal(123, loadedTempData["Key2"]);
        Assert.Equal(true, loadedTempData["Key3"]);
    }

    [Fact]
    public void Save_ThrowsForUnsupportedType()
    {
        var httpContext = CreateHttpContext();
        var tempData = new TempData();
        tempData["Key"] = new object();

        Assert.Throws<InvalidOperationException>(() => cookieTempDataProvider.SaveTempData(httpContext, tempData.Save()));
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_ForInvalidBase64Cookie()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Cookie"] = ".AspNetCore.Components.TempData=not-valid-base64!!!";
        var tempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_ForUnsupportedType()
    {
        var httpContext = CreateHttpContext();
        var json = "{\"Key\":[true, false, true]}";
        var encoded = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(json));
        httpContext.Request.Headers["Cookie"] = $".AspNetCore.Components.TempData={encoded}";
        var tempData = cookieTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection()
            .AddSingleton<IDataProtectionProvider, PassThroughDataProtectionProvider>()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost");

        var cookieFeature = new TestResponseCookiesFeature();
        httpContext.Features.Set(cookieFeature);
        httpContext.Features.Set<IResponseCookiesFeature>(cookieFeature);

        return httpContext;
    }

    private static void SimulateCookieRoundTrip(HttpContext httpContext)
    {
        var cookieFeature = httpContext.Features.Get<TestResponseCookiesFeature>();
        if (cookieFeature != null && cookieFeature.SetCookies.TryGetValue(".AspNetCore.Components.TempData", out var cookieValue))
        {
            httpContext.Request.Headers["Cookie"] = $".AspNetCore.Components.TempData={cookieValue}";
        }
    }

    private class PassThroughDataProtectionProvider : IDataProtectionProvider
    {
        public IDataProtector CreateProtector(string purpose) => new PassThroughDataProtector();

        private class PassThroughDataProtector : IDataProtector
        {
            public IDataProtector CreateProtector(string purpose) => this;
            public byte[] Protect(byte[] plaintext) => plaintext;
            public byte[] Unprotect(byte[] protectedData) => protectedData;
        }
    }

    private class TestResponseCookiesFeature : IResponseCookiesFeature
    {
        public Dictionary<string, string> SetCookies { get; } = new();
        public HashSet<string> DeletedCookies { get; } = new();

        public IResponseCookies Cookies => new TestResponseCookies(this);

        private class TestResponseCookies : IResponseCookies
        {
            private readonly TestResponseCookiesFeature _feature;

            public TestResponseCookies(TestResponseCookiesFeature feature)
            {
                _feature = feature;
            }

            public void Append(string key, string value) => Append(key, value, new CookieOptions());

            public void Append(string key, string value, CookieOptions options)
            {
                // ChunkingCookieManager deletes by appending with expired date (UnixEpoch)
                if (options.Expires.HasValue && options.Expires.Value <= DateTimeOffset.UnixEpoch)
                {
                    _feature.DeletedCookies.Add(key);
                }
                else
                {
                    _feature.SetCookies[key] = value;
                }
            }

            public void Delete(string key) => Delete(key, new CookieOptions());

            public void Delete(string key, CookieOptions options)
            {
                _feature.DeletedCookies.Add(key);
            }
        }
    }
}
