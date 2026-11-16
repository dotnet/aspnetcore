// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class AuthorizeRolesBenchmark
{
    private IAuthorizationService _authorizationService;
    private AuthorizationPolicy _rolesPolicy;
    private AuthorizationPolicy _spacedRolesPolicy;
    private ClaimsPrincipal _user;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddAuthorization()
            .BuildServiceProvider();

        _authorizationService = services.GetRequiredService<IAuthorizationService>();
        var policyProvider = services.GetRequiredService<IAuthorizationPolicyProvider>();

        // The combined policy is cached per endpoint by AuthorizationMiddleware, so only evaluation is measured.
        _rolesPolicy = AuthorizationPolicy.CombineAsync(policyProvider, [new AuthorizeAttribute { Roles = "Admin,User" }]).GetAwaiter().GetResult();
        _spacedRolesPolicy = AuthorizationPolicy.CombineAsync(policyProvider, [new AuthorizeAttribute { Roles = "Manager, Editor, User" }]).GetAwaiter().GetResult();

        _user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "user"),
                new Claim(ClaimTypes.Role, "User"),
            ],
            "Test"));
    }

    [Benchmark]
    public Task<AuthorizationResult> AuthorizeAsync_Roles()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _rolesPolicy);

    [Benchmark]
    public Task<AuthorizationResult> AuthorizeAsync_RolesWithSpaces()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _spacedRolesPolicy);
}
