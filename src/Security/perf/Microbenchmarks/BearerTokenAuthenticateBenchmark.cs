// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class BearerTokenAuthenticateBenchmark
{
    private const string Scheme = "Identity.Bearer";

    private IServiceScopeFactory _scopeFactory;
    private string _authorizationHeader;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAuthentication().AddBearerToken(Scheme);
        var serviceProvider = services.BuildServiceProvider();
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // Shape of a typical ASP.NET Core Identity access token.
        var options = serviceProvider.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(Scheme);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "6f1b6c2e-43c5-4d8f-9a55-3b7f0c1f2a9d"),
                new Claim(ClaimTypes.Name, "someone@example.com"),
                new Claim(ClaimTypes.Email, "someone@example.com"),
                new Claim("AspNet.Identity.SecurityStamp", "QWERTYUIOPASDFGHJKLZXCVBNM234567"),
                new Claim("amr", "pwd"),
            ],
            "Identity.Application"));
        var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) };
        _authorizationHeader = "Bearer " + options.BearerTokenProtector.Protect(new AuthenticationTicket(principal, properties, $"{Scheme}:AccessToken"));
    }

    [Benchmark]
    public async Task<AuthenticateResult> AuthenticateAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var httpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        httpContext.Request.Headers.Authorization = _authorizationHeader;
        return await httpContext.AuthenticateAsync(Scheme);
    }
}
