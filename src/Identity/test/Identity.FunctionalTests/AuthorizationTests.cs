// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Identity.FunctionalTests;

public abstract class AuthorizationTests<TStartup, TContext> : IClassFixture<ServerFactory<TStartup, TContext>>
    where TStartup : class
    where TContext : DbContext
{
    protected AuthorizationTests(ServerFactory<TStartup, TContext> serverFactory)
    {
        ServerFactory = serverFactory;
    }

    public ServerFactory<TStartup, TContext> ServerFactory { get; }

    public static TheoryData<string> AuthorizedPages =>
        new TheoryData<string>
        {
                "/Identity/Account/Manage/ChangePassword",
                "/Identity/Account/Manage/DeletePersonalData",
                "/Identity/Account/Manage/Disable2fa",
                "/Identity/Account/Manage/DownloadPersonalData",
                "/Identity/Account/Manage/EnableAuthenticator",
                "/Identity/Account/Manage/ExternalLogins",
                "/Identity/Account/Manage/GenerateRecoveryCodes",
                "/Identity/Account/Manage/Index",
                "/Identity/Account/Manage/PersonalData",
                "/Identity/Account/Manage/ResetAuthenticator",
                "/Identity/Account/Manage/SetPassword",
                "/Identity/Account/Manage/ShowRecoveryCodes",
                "/Identity/Account/Manage/TwoFactorAuthentication",
        };

    [Theory]
    [MemberData(nameof(AuthorizedPages))]
    public async Task AnonymousUserCantAccessAuthorizedPages(string url)
    {
        // Arrange
        var client = ServerFactory
            .CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        var location = ResponseAssert.IsRedirect(response);
        Assert.StartsWith("/Identity/Account/Login?", location.PathAndQuery);
    }

    // The routes commented below are not directly accessible by
    // typing the URL in the browser. They have to be accessed as
    // part of a more complex interation. (like disable 2fa).
    // /Identity/Account/Manage/Disable2fa
    // /Identity/Account/Manage/GenerateRecoveryCodes
    // /Identity/Account/Manage/SetPassword
    // /Identity/Account/Manage/ShowRecoveryCodes
    public static TheoryData<string> RouteableAuthorizedPages =>
        new TheoryData<string>
        {
                "/Identity/Account/Manage/ChangePassword",
                "/Identity/Account/Manage/DeletePersonalData",
                "/Identity/Account/Manage/EnableAuthenticator",
                "/Identity/Account/Manage/ExternalLogins",
                "/Identity/Account/Manage/Index",
                "/Identity/Account/Manage/PersonalData",
                "/Identity/Account/Manage/ResetAuthenticator",
                "/Identity/Account/Manage/TwoFactorAuthentication",
                "/Identity/Account/Logout",
        };

    [Theory]
    [MemberData(nameof(RouteableAuthorizedPages))]
    public async Task AuthenticatedUserCanAccessAuthorizedPages(string url)
    {
        // Arrange
        var client = ServerFactory
            .CreateClient();

        await UserStories.RegisterNewUserAsync(client);

        // Act
        var response = await client.GetAsync(url);

        // Assert
        await ResponseAssert.IsHtmlDocumentAsync(response);
    }

    // The routes commented below are not directly accessible by
    // typing the URL in the browser. They have to be accessed as
    // part of a more complex interation. (like login with 2fa).
    // /Identity/Account/LoginWithRecoveryCode
    // /Identity/Account/LoginWith2fa
    // /Identity/Account/ExternalLogin
    // /Identity/Account/ConfirmEmail
    // /Identity/Account/ResetPassword,
    public static TheoryData<string> UnauthorizedPages =>
        new TheoryData<string>
        {
                "/Identity/Account/Login",
                "/Identity/Account/Lockout",
                "/Identity/Account/ForgotPasswordConfirmation",
                "/Identity/Account/ForgotPassword",
                "/Identity/Account/AccessDenied",
        };

    [Theory]
    [MemberData(nameof(UnauthorizedPages))]
    public async Task AnonymousUserCanAccessNotAuthorizedPages(string url)
    {
        // Arrange
        var client = ServerFactory
            .CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        await ResponseAssert.IsHtmlDocumentAsync(response);
    }

    public static TheoryData<string> UnauthorizedPagesAllowAnonymous =>
    new TheoryData<string>
    {
             "/Identity/Error",
             "/Identity/Account/Register",
             "/Identity/Account/Login",
             "/Identity/Account/ForgotPassword",
             "/Identity/Account/Logout"
    };

    [Theory]
    [MemberData(nameof(UnauthorizedPagesAllowAnonymous))]
    public async Task AnonymousUserAllowedAccessToPages_WithGlobalAuthorizationFilter(string url)
    {
        // Arrange
        void TestServicesConfiguration(IServiceCollection services) =>
            services.SetupGlobalAuthorizeFilter();

        var client = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(TestServicesConfiguration))
            .CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        await ResponseAssert.IsHtmlDocumentAsync(response);
    }
}
