// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Test;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Microsoft.AspNetCore.Identity.InMemory.Test;

public class ControllerTest
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task VerifyAccountControllerSignIn(bool isPersistent)
    {
        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        auth.Setup(a => a.SignInAsync(context, IdentityConstants.ApplicationScheme,
            It.IsAny<ClaimsPrincipal>(),
            It.IsAny<AuthenticationProperties>())).Returns(Task.FromResult(0)).Verifiable();
        // REVIEW: is persistant mocking broken
        //It.Is<AuthenticationProperties>(v => v.IsPersistent == isPersistent))).Returns(Task.FromResult(0)).Verifiable();
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(context);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddLogging()
            .AddSingleton(contextAccessor.Object);

        services.AddIdentity<PocoUser, PocoRole>();
        services.AddSingleton<IUserStore<PocoUser>, InMemoryStore<PocoUser, PocoRole>>();
        services.AddSingleton<IRoleStore<PocoRole>, InMemoryStore<PocoUser, PocoRole>>();

        var app = new ApplicationBuilder(services.BuildServiceProvider());

        // Act
        var user = new PocoUser
        {
            UserName = "Yolo"
        };
        const string password = "[PLACEHOLDER]-1a";
        var userManager = app.ApplicationServices.GetRequiredService<UserManager<PocoUser>>();
        var signInManager = app.ApplicationServices.GetRequiredService<SignInManager<PocoUser>>();

        IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));

        var result = await signInManager.PasswordSignInAsync(user, password, isPersistent, false);

        // Assert
        Assert.True(result.Succeeded);
        auth.VerifyAll();
        contextAccessor.VerifyAll();
    }

    [Fact]
    public async Task VerifyAccountControllerExternalLoginWithTokensFlow()
    {
        // Setup the external cookie like it would look from a real OAuth2
        var externalId = "<externalId>";
        var authScheme = "<authScheme>";
        var externalIdentity = new ClaimsIdentity();
        externalIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, externalId));
        var externalPrincipal = new ClaimsPrincipal(externalIdentity);
        var externalLogin = new ExternalLoginInfo(externalPrincipal, authScheme, externalId, "displayname")
        {
            AuthenticationTokens = new[] {
                    new AuthenticationToken { Name = "refresh_token", Value = "refresh" },
                    new AuthenticationToken { Name = "access_token", Value = "access" }
                }
        };

        var context = new DefaultHttpContext();
        var auth = MockAuth(context);
        auth.Setup(a => a.AuthenticateAsync(context, It.IsAny<string>())).Returns(Task.FromResult(AuthenticateResult.NoResult()));
        var contextAccessor = new Mock<IHttpContextAccessor>();
        contextAccessor.Setup(a => a.HttpContext).Returns(context);
        var services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddLogging()
            .AddSingleton(contextAccessor.Object);
        services.AddIdentity<PocoUser, PocoRole>();
        services.AddSingleton<IUserStore<PocoUser>, InMemoryStore<PocoUser, PocoRole>>();
        services.AddSingleton<IRoleStore<PocoRole>, InMemoryStore<PocoUser, PocoRole>>();

        var app = new ApplicationBuilder(services.BuildServiceProvider());

        // Act
        var user = new PocoUser
        {
            UserName = "Yolo"
        };
        var userManager = app.ApplicationServices.GetRequiredService<UserManager<PocoUser>>();
        var signInManager = app.ApplicationServices.GetRequiredService<SignInManager<PocoUser>>();

        IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
        IdentityResultAssert.IsSuccess(await userManager.AddLoginAsync(user, new UserLoginInfo(authScheme, externalId, "whatever")));
        IdentityResultAssert.IsSuccess(await signInManager.UpdateExternalAuthenticationTokensAsync(externalLogin));
        Assert.Equal("refresh", await userManager.GetAuthenticationTokenAsync(user, authScheme, "refresh_token"));
        Assert.Equal("access", await userManager.GetAuthenticationTokenAsync(user, authScheme, "access_token"));
    }

    private Mock<IAuthenticationService> MockAuth(HttpContext context)
    {
        var auth = new Mock<IAuthenticationService>();
        context.RequestServices = new ServiceCollection().AddSingleton(auth.Object).BuildServiceProvider();
        return auth;
    }
}
