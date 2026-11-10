// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Security;

public class AuthorizationServiceBenchmark
{
    private IAuthorizationService _authorizationService;
    private AuthorizationPolicy _defaultPolicy;
    private AuthorizationPolicy _claimsPolicy;
    private ClaimsPrincipal _user;
    private AuthorizationMiddleware _authorizationMiddleware;
    private DefaultHttpContext _httpContext;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource())
            .AddAuthorization(options =>
            {
                options.AddPolicy("Claims", policy => policy.RequireClaim("scope", "read").RequireRole("admin"));
            })
            .BuildServiceProvider();

        _authorizationService = services.GetRequiredService<IAuthorizationService>();
        var policyProvider = services.GetRequiredService<IAuthorizationPolicyProvider>();
        _defaultPolicy = policyProvider.GetDefaultPolicyAsync().GetAwaiter().GetResult();
        _claimsPolicy = policyProvider.GetPolicyAsync("Claims").GetAwaiter().GetResult();

        _user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "user"),
                new Claim(ClaimTypes.Role, "admin"),
                new Claim("scope", "read"),
            ],
            "Test"));

        _authorizationMiddleware = new AuthorizationMiddleware(_ => Task.CompletedTask, policyProvider, services);

        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new AuthorizeAttribute()), "Authorized endpoint");
        _httpContext = new DefaultHttpContext { RequestServices = services };
        _httpContext.SetEndpoint(endpoint);
        var authFeature = new AuthenticationFeature { User = _user, AuthenticateResult = AuthenticateResult.Success(new AuthenticationTicket(_user, "Test")) };
        _httpContext.Features.Set<IAuthenticateResultFeature>(authFeature);
        _httpContext.Features.Set<IHttpAuthenticationFeature>(authFeature);
    }

    [Benchmark]
    public Task<AuthorizationResult> AuthorizeAsync_DefaultPolicy()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _defaultPolicy);

    [Benchmark]
    public Task<AuthorizationResult> AuthorizeAsync_ClaimsAndRolePolicy()
        => _authorizationService.AuthorizeAsync(_user, resource: null, _claimsPolicy);

    [Benchmark]
    public Task Invoke_HasEndpoint_Authorize()
        => _authorizationMiddleware.Invoke(_httpContext);

    private sealed class AuthenticationFeature : IAuthenticateResultFeature, IHttpAuthenticationFeature
    {
        public AuthenticateResult AuthenticateResult { get; set; }

        public ClaimsPrincipal User { get; set; }
    }
}
