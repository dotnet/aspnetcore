// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class ClaimsAuthorizationRequirementBenchmark
{
    private IAuthorizationService _authorizationService;
    private ClaimsPrincipal _user;
    private AuthorizationPolicy _requireClaimWithValues;
    private AuthorizationPolicy _requireClaim;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore();
        _authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();

        var claims = new List<Claim> { new(ClaimTypes.Name, "bob"), new(ClaimTypes.NameIdentifier, "123") };
        for (var i = 0; i < 10; i++)
        {
            claims.Add(new Claim("custom" + i, "v" + i));
        }
        claims.Add(new Claim(ClaimTypes.Role, "User"));
        claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        claims.Add(new Claim("permission", "read"));
        claims.Add(new Claim("permission", "write"));
        _user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies"));

        _requireClaimWithValues = new AuthorizationPolicyBuilder().RequireClaim("permission", "admin", "write").Build();
        _requireClaim = new AuthorizationPolicyBuilder().RequireClaim("permission").Build();
    }

    [Benchmark]
    public Task<AuthorizationResult> RequireClaimWithValues()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _requireClaimWithValues);

    [Benchmark]
    public Task<AuthorizationResult> RequireClaim()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _requireClaim);
}
