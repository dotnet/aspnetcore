// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class DefaultAuthorizationServiceBenchmark
{
    private IAuthorizationService _authorizationService;
    private ClaimsPrincipal _user;
    private AuthorizationPolicy _requireAuthenticatedUser;
    private AuthorizationPolicy _requireRole;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorizationCore();
        _authorizationService = services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();

        _user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, "bob"), new Claim(ClaimTypes.Role, "Admin") },
            "Cookies"));

        _requireAuthenticatedUser = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
        _requireRole = new AuthorizationPolicyBuilder().RequireRole("Admin").Build();
    }

    [Benchmark]
    public Task<AuthorizationResult> AuthorizeAsync_RequireAuthenticatedUser()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _requireAuthenticatedUser);

    [Benchmark]
    public Task<AuthorizationResult> AuthorizeAsync_RequireRole()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _requireRole);
}
