// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Extensions;

public class DefaultClientRequestParametersProviderTests
{
    class NameService : IIssuerNameService
    {
        public Task<string> GetCurrentAsync() => Task.FromResult("http://localhost");
    }

    [Fact]
    public void GetClientParameters_ReturnsParametersForExistingClients()
    {
        // Arrange
        var absoluteUrlFactory = new Mock<IAbsoluteUrlFactory>();
        absoluteUrlFactory.Setup(auf => auf.GetAbsoluteUrl(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .Returns<HttpContext, string>((_, s) => Uri.IsWellFormedUriString(s, UriKind.Absolute) ? s : new Uri(new Uri("http://localhost/"), s).ToString());

        var options = Options.Create(new ApiAuthorizationOptions());
        options.Value.Clients.AddIdentityServerSPA("SPA", cb =>
             cb.WithScopes("a/b", "c/d")
                .WithRedirectUri("authentication/login-callback")
                .WithLogoutRedirectUri("authentication/logout-callback"));

        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost");
        context.RequestServices = new ServiceCollection()
            .AddSingleton(new IdentityServerOptions())
            .AddSingleton<IIssuerNameService>(new NameService())
            .BuildServiceProvider();

        var clientRequestParametersProvider =
            new DefaultClientRequestParametersProvider(
                absoluteUrlFactory.Object,
                options);

        var expectedParameters = new Dictionary<string, string>
        {
            ["authority"] = "http://localhost",
            ["client_id"] = "SPA",
            ["redirect_uri"] = "http://localhost/authentication/login-callback",
            ["post_logout_redirect_uri"] = "http://localhost/authentication/logout-callback",
            ["response_type"] = "code",
            ["scope"] = "a/b c/d"
        };

        // Act
        var result = clientRequestParametersProvider.GetClientParameters(context, "SPA");

        // Assert
        Assert.Equal(expectedParameters, result);
    }
}
