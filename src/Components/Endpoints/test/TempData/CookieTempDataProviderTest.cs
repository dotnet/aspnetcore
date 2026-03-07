// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class CookieTempDataProviderTest
{
    private readonly CookieTempDataProvider _cookieTempDataProvider;

    internal TempData CreateTempData()
    {
        return new TempData(() => new Dictionary<string, (object Value, Type Type)>());
    }

    public CookieTempDataProviderTest()
    {
        _cookieTempDataProvider = new CookieTempDataProvider(
            new EphemeralDataProtectionProvider(),
            Options.Create<RazorComponentsServiceOptions>(new()),
            new JsonTempDataSerializer(),
            NullLogger<CookieTempDataProvider>.Instance);
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_WhenNoCookieExists()
    {
        var httpContext = CreateHttpContext();
        var tempData = _cookieTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
    }

    [Fact]
    public void Load_ReturnsEmptyTempData_AndClearsCookie_WhenDataIsInvalid()
    {
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers["Cookie"] = ".AspNetCore.Components.TempData=not-valid-base64!!!";

        var tempData = _cookieTempDataProvider.LoadTempData(httpContext);

        Assert.NotNull(tempData);
        Assert.Empty(tempData);
        // Cookie should be deleted when invalid
        var cookieFeature = httpContext.Features.Get<TestResponseCookiesFeature>();
        Assert.NotNull(cookieFeature);
        Assert.Contains(".AspNetCore.Components.TempData", cookieFeature.DeletedCookies);
    }

    [Fact]
    public void Save_DeletesCookie_WhenNoDataToSave()
    {
        var httpContext = CreateHttpContext();
        var tempData = CreateTempData();

        _cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());

        var cookieFeature = httpContext.Features.Get<TestResponseCookiesFeature>();
        Assert.NotNull(cookieFeature);
        Assert.Contains(".AspNetCore.Components.TempData", cookieFeature.DeletedCookies);
    }

    [Fact]
    public void Save_SetsCookie_WhenDataExists()
    {
        var httpContext = CreateHttpContext();
        var tempData = CreateTempData();
        tempData["Key1"] = "Value1";

        _cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());

        var cookieFeature = httpContext.Features.Get<TestResponseCookiesFeature>();
        Assert.NotNull(cookieFeature);
        Assert.Contains(".AspNetCore.Components.TempData", cookieFeature.SetCookies.Keys);
    }

    [Fact]
    public void Save_ThrowsForUnsupportedType()
    {
        var httpContext = CreateHttpContext();
        var tempData = CreateTempData();
        tempData["Key"] = new object();

        Assert.Throws<InvalidOperationException>(() => _cookieTempDataProvider.SaveTempData(httpContext, tempData.Save()));
    }

    [Fact]
    public void RoundTrip_SaveAndLoad_WorksCorrectly()
    {
        var httpContext = CreateHttpContext();
        var tempData = CreateTempData();
        tempData["StringKey"] = "StringValue";
        tempData["IntKey"] = 42;

        _cookieTempDataProvider.SaveTempData(httpContext, tempData.Save());
        SimulateCookieRoundTrip(httpContext);
        var loadedTempData = _cookieTempDataProvider.LoadTempData(httpContext);

        Assert.Equal("StringValue", loadedTempData["StringKey"].Value);
        Assert.Equal(42, loadedTempData["IntKey"].Value);
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
