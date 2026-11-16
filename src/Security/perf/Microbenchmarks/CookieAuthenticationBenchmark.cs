// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class CookieAuthenticationBenchmark
{
    private IServiceProvider _services;
    private IServiceScopeFactory _scopeFactory;
    private string _cookieHeader;
    private string _chunkedCookieHeader;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddDataProtection().UseEphemeralDataProtectionProvider();
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
        services.AddAuthentication().AddCookie("chunked", o => o.Cookie.Name = "Big");
        _services = services.BuildServiceProvider();
        _scopeFactory = _services.GetRequiredService<IServiceScopeFactory>();

        _cookieHeader = SignIn(CookieAuthenticationDefaults.AuthenticationScheme, 10);
        _chunkedCookieHeader = SignIn("chunked", 120);

        if (!Authenticate().GetAwaiter().GetResult() || !AuthenticateChunked().GetAwaiter().GetResult())
        {
            throw new InvalidOperationException("Authentication failed.");
        }
    }

    private string SignIn(string scheme, int claimCount)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "bob"), new Claim(ClaimTypes.NameIdentifier, "0f8fad5b-d9cb-469f-a165-70867728950e") };
        for (var i = 0; i < claimCount; i++)
        {
            claims.Add(new Claim("custom-claim-" + i, "value-" + i + "-" + new string('x', 20)));
        }
        using var scope = _services.CreateScope();
        var context = NewContext(scope.ServiceProvider);
        context.SignInAsync(scheme, new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))).GetAwaiter().GetResult();
        var setCookies = context.Response.Headers.SetCookie;
        return string.Join("; ", setCookies.Select(c => c.Substring(0, c.IndexOf(';'))));
    }

    private static DefaultHttpContext NewContext(IServiceProvider services)
    {
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("localhost");
        context.Request.Path = "/";
        return context;
    }

    [Benchmark]
    public async Task<bool> Authenticate()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = NewContext(scope.ServiceProvider);
        context.Request.Headers.Cookie = _cookieHeader;
        var result = await context.AuthenticateAsync();
        return result.Succeeded;
    }

    [Benchmark]
    public async Task<bool> AuthenticateChunked()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = NewContext(scope.ServiceProvider);
        context.Request.Headers.Cookie = _chunkedCookieHeader;
        var result = await context.AuthenticateAsync("chunked");
        return result.Succeeded;
    }

    [Benchmark]
    public async Task<bool> AuthenticateNoCookie()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = NewContext(scope.ServiceProvider);
        var result = await context.AuthenticateAsync();
        return result.Succeeded;
    }
}
