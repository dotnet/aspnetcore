// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    public async void ShouldUseExpirationDateFromAuthenticationPropertiesInsteadOfTokenOptions_WhenItIsSpecified()
    {
        // Arrange
        var expiresUtc = DateTime.UtcNow + TimeSpan.FromHours(8);
        using var host = await CreateHost(
            options => options.BearerTokenExpiration = TimeSpan.FromDays(-1),
            new AuthenticationProperties { ExpiresUtc = expiresUtc }
        );
        using var server = host.GetTestServer();

        // Act
        var signInTransaction = await SendAsync(server, "http://example.com/signIn");
        var accessTokenResponse = JsonSerializer.Deserialize<AccessTokenResponse>(signInTransaction.ResponseText,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var oauthTransaction = await SendAsync(server, "http://example.com/oauth", $"Bearer {accessTokenResponse.AccessToken}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, signInTransaction.Response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, oauthTransaction.Response.StatusCode);
        Assert.Equal("true", oauthTransaction.ResponseText);
        Assert.True(accessTokenResponse.ExpiresIn > 0);
    }

    private static async Task<IHost> CreateHost(Action<BearerTokenOptions> configure, AuthenticationProperties props = null)
    {
        var host = new HostBuilder()
            .ConfigureWebHost(builder =>
            {
                builder
                    .UseTestServer()
                    .Configure(builder =>
                    {
                        builder.UseAuthentication();
                        builder.Use(async (HttpContext context, RequestDelegate next) =>
                        {
                            if (context.Request.Path == new PathString("/signIn"))
                            {
                                await context.SignInAsync(
                                    BearerTokenDefaults.AuthenticationScheme,
                                    new ClaimsPrincipal(new ClaimsIdentity("mock")),
                                    props);
                                return;
                            }
                            if (context.Request.Path == new PathString("/oauth"))
                            {
                                var authenticationResult = await context.AuthenticateAsync(BearerTokenDefaults.AuthenticationScheme);
                                await context.Response.WriteAsJsonAsync(authenticationResult.Succeeded);
                                return;
                            }
                        });
                    })
                    .ConfigureServices(services =>
                    {
                        services
                            .AddAuthentication(BearerTokenDefaults.AuthenticationScheme)
                            .AddBearerToken(BearerTokenDefaults.AuthenticationScheme, configure)
                            ;
                    });
            })
            .Build();

        await host.StartAsync();
        return host;
    }

    private static async Task<Transaction> SendAsync(TestServer server, string uri, string authorizationHeader = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!string.IsNullOrEmpty(authorizationHeader))
        {
            request.Headers.Add("Authorization", authorizationHeader);
        }
        var transaction = new Transaction
        {
            Request = request,
            Response = await server.CreateClient().SendAsync(request),
        };
        transaction.ResponseText = await transaction.Response.Content.ReadAsStringAsync();
        return transaction;
    }
}
