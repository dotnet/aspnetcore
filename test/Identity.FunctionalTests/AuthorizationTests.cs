// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class AuthorizationTests : LoggedTest, IClassFixture<ServerFactory>
    {
        public AuthorizationTests(ServerFactory serverFactory, ITestOutputHelper output) : base(output)
        {
            ServerFactory = serverFactory;
        }

        public ServerFactory ServerFactory { get; }

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
            using (StartLog(out var loggerFactory, $"{nameof(AnonymousUserCantAccessAuthorizedPages)}_{WebUtility.UrlEncode(url)}"))
            {
                // Arrange
                var client = ServerFactory.CreateDefaultClient(loggerFactory);

                // Act
                var response = await client.GetAsync(url);

                // Assert
                var location = ResponseAssert.IsRedirect(response);
                Assert.StartsWith("/Identity/Account/Login?", location.PathAndQuery);
            }
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
                "/Identity/Account/Manage/DownloadPersonalData",
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
            using (StartLog(out var loggerFactory, $"{nameof(AuthenticatedUserCanAccessAuthorizedPages)}_{WebUtility.UrlEncode(url)}"))
            {
                // Arrange
                var client = ServerFactory.CreateDefaultClient(loggerFactory);
                await UserStories.RegisterNewUserAsync(client);

                // Act
                var response = await client.GetAsync(url);

                // Assert
                await ResponseAssert.IsHtmlDocumentAsync(response);
            }
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
            using (StartLog(out var loggerFactory, $"{nameof(AnonymousUserCanAccessNotAuthorizedPages)}_{WebUtility.UrlEncode(url)}"))
            {
                // Arrange
                var client = ServerFactory.CreateDefaultClient(loggerFactory);

                // Act
                var response = await client.GetAsync(url);

                // Assert
                await ResponseAssert.IsHtmlDocumentAsync(response);
            }
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
            using (StartLog(out var loggerFactory, $"{nameof(AnonymousUserAllowedAccessToPages_WithGlobalAuthorizationFilter)}_{WebUtility.UrlEncode(url)}"))
            {
                // Arrange
                var server = ServerFactory.CreateServer(loggerFactory, builder =>
                   builder.ConfigureServices(services => services.SetupGlobalAuthorizeFilter()));
                var client = ServerFactory.CreateDefaultClient(server);

                // Act
                var response = await client.GetAsync(url);

                // Assert
                await ResponseAssert.IsHtmlDocumentAsync(response);
            }
        }
    }
}
