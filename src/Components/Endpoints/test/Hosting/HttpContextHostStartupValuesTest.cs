// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.Hosting;

public class HttpContextHostStartupValuesTest
{
    [Fact]
    public void GetValueRejectsNullKeyBeforeInitialization()
    {
        var startupValues = new HttpContextHostStartupValues([]);

        Assert.Throws<ArgumentNullException>(() => startupValues.GetValue(null!));
    }

    [Fact]
    public void CollectsProviderValues()
    {
        var startupValues = new HttpContextHostStartupValues(
        [
            new TestHttpContextStartupValueProvider("first", "one"),
            new TestHttpContextStartupValueProvider("second", "two"),
        ]);

        startupValues.Initialize(new DefaultHttpContext());

        Assert.Equal("one", startupValues.GetValue("first"));
        Assert.Equal("two", startupValues.GetRequired("second"));
        Assert.Null(startupValues.GetValue("missing"));
        var exception = Assert.Throws<InvalidOperationException>(() => startupValues.GetRequired("missing"));
        Assert.Equal("Startup value 'missing' was not provided.", exception.Message);
    }

    [Fact]
    public void RejectsDuplicateProviderValuesEvenWhenValuesMatch()
    {
        var startupValues = new HttpContextHostStartupValues(
        [
            new TestHttpContextStartupValueProvider("duplicate", "value"),
            new TestHttpContextStartupValueProvider("duplicate", "value"),
        ]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => startupValues.Initialize(new DefaultHttpContext()));

        Assert.Equal("The startup value key 'duplicate' was provided more than once.", exception.Message);
    }

    [Fact]
    public void DefaultProviderUsesRequestUris()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");
        httpContext.Request.PathBase = "/base";
        httpContext.Request.Path = "/page";
        httpContext.Request.QueryString = new QueryString("?key=value");

        var values = new NavigationHttpContextStartupValueProvider().GetValues(httpContext);

        Assert.Equal("https://example.com/base/", values["document.baseURI"]);
        Assert.Equal("https://example.com/base/page?key=value", values["location.href"]);
    }

    [Fact]
    public void AddRazorComponentsRegistersAndCollectsAllProviders()
    {
        var services = new ServiceCollection();
        services.AddRazorComponents();
        services.AddSingleton<IHttpContextStartupValueProvider>(
            new TestHttpContextStartupValueProvider("custom", "expected"));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
        };
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("example.com");
        var holder = scope.ServiceProvider
            .GetRequiredKeyedService<HttpContextHostStartupValues>(HostInitializerKey.Static);

        holder.Initialize(httpContext);

        Assert.Equal("expected", scope.ServiceProvider.GetRequiredService<IHostStartupValues>().GetRequired("custom"));
        Assert.Equal("https://example.com/", holder.GetRequired("document.baseURI"));
    }

    private sealed class TestHttpContextStartupValueProvider(string key, string value) : IHttpContextStartupValueProvider
    {
        public IReadOnlyDictionary<string, string> GetValues(HttpContext httpContext)
            => new Dictionary<string, string>
            {
                [key] = value,
            };
    }
}
