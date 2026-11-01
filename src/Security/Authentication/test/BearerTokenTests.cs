// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication.BearerToken;

public class BearerTokenTests : SharedAuthenticationTests<BearerTokenOptions>
{
    protected override string DefaultScheme => BearerTokenDefaults.AuthenticationScheme;

    protected override Type HandlerType
    {
        get
        {
            var services = new ServiceCollection();
            services.AddAuthentication().AddBearerToken();
            return services.Select(d => d.ServiceType).Single(typeof(AuthenticationHandler<BearerTokenOptions>).IsAssignableFrom);
        }
    }

    protected override void RegisterAuth(AuthenticationBuilder services, Action<BearerTokenOptions> configure)
    {
        services.AddBearerToken(configure);
    }

    [Fact]
    public void EventsPropertyIsInitializedOnConstruction()
    {
        var options = new BearerTokenOptions();
        Assert.NotNull(options.Events);
    }

    [Fact]
    public async Task AuthenticatesValidTokenFromAuthorizationHeader()
    {
        using var services = CreateServices();
        var token = CreateToken(services, DateTimeOffset.UtcNow.AddHours(1));

        var result = await AuthenticateAsync(services, "Bearer " + token);

        Assert.True(result.Succeeded);
        Assert.Equal("Alice", result.Principal.Identity.Name);
    }

    [Fact]
    public async Task AuthenticatesTokenEncodedAsStandardBase64()
    {
        using var services = CreateServices();
        string token;
        do
        {
            token = CreateToken(services, DateTimeOffset.UtcNow.AddHours(1));
        }
        while (!token.Contains('-') && !token.Contains('_'));

        var base64Token = token.Replace('-', '+').Replace('_', '/');
        base64Token = base64Token.PadRight(base64Token.Length + ((4 - base64Token.Length % 4) % 4), '=');

        var result = await AuthenticateAsync(services, "Bearer " + base64Token);

        Assert.True(result.Succeeded);
        Assert.Equal("Alice", result.Principal.Identity.Name);
    }

    [Fact]
    public async Task FailsExpiredToken()
    {
        using var services = CreateServices();
        var token = CreateToken(services, DateTimeOffset.UtcNow.AddHours(-1));

        var result = await AuthenticateAsync(services, "Bearer " + token);

        Assert.False(result.Succeeded);
        Assert.Equal("Token expired", result.Failure.Message);
    }

    [Theory]
    [InlineData("Bearer ")]
    [InlineData("Bearer invalid")]
    [InlineData("Bearer in+valid")]
    [InlineData("Bearer in-valid_")]
    public async Task FailsInvalidToken(string authorization)
    {
        using var services = CreateServices();

        var result = await AuthenticateAsync(services, authorization);

        Assert.False(result.Succeeded);
        Assert.Equal("Unprotected token failed", result.Failure.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Basic abc")]
    [InlineData("bearer abc")]
    public async Task ReturnsNoResultWithoutBearerAuthorizationHeader(string authorization)
    {
        using var services = CreateServices();

        var result = await AuthenticateAsync(services, authorization);

        Assert.True(result.None);
    }

    [Fact]
    public async Task UsesCustomBearerTokenProtector()
    {
        var protector = new RecordingTicketDataFormat();
        using var services = CreateServices(o => o.BearerTokenProtector = protector);

        var result = await AuthenticateAsync(services, "Bearer custom-token");

        Assert.True(result.Succeeded);
        Assert.Equal("custom-token", protector.LastProtectedText);
    }

    private static ServiceProvider CreateServices(Action<BearerTokenOptions> configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAuthentication().AddBearerToken(o => configure?.Invoke(o));
        return services.BuildServiceProvider();
    }

    private static string CreateToken(IServiceProvider services, DateTimeOffset expiresUtc)
    {
        var options = services.GetRequiredService<IOptionsMonitor<BearerTokenOptions>>().Get(BearerTokenDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Alice")], "Test"));
        var properties = new AuthenticationProperties { ExpiresUtc = expiresUtc };
        return options.BearerTokenProtector.Protect(new AuthenticationTicket(principal, properties, BearerTokenDefaults.AuthenticationScheme));
    }

    private static async Task<AuthenticateResult> AuthenticateAsync(ServiceProvider services, string authorization)
    {
        await using var scope = services.CreateAsyncScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        if (authorization is not null)
        {
            context.Request.Headers.Authorization = authorization;
        }

        return await context.AuthenticateAsync(BearerTokenDefaults.AuthenticationScheme);
    }

    private sealed class RecordingTicketDataFormat : ISecureDataFormat<AuthenticationTicket>
    {
        public string LastProtectedText { get; private set; }

        public string Protect(AuthenticationTicket data) => throw new NotSupportedException();

        public string Protect(AuthenticationTicket data, string purpose) => throw new NotSupportedException();

        public AuthenticationTicket Unprotect(string protectedText) => Unprotect(protectedText, purpose: null);

        public AuthenticationTicket Unprotect(string protectedText, string purpose)
        {
            LastProtectedText = protectedText;
            var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Bob")], "Test"));
            return new AuthenticationTicket(principal, new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1) }, "Test");
        }
    }
}
