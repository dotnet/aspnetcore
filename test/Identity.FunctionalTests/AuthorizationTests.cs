// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class AuthorizationTests : IClassFixture<ServerFactory>
    {
        public AuthorizationTests(ServerFactory serverFactory)
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
                "/Identity/Account/Logout",
            };

        [Theory]
        [MemberData(nameof(AuthorizedPages))]
        public async Task AnonymousUserCantAccessAuthorizedPages(string url)
        {
            // Arrange
            var client = ServerFactory.CreateDefaultClient();

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
            // Arrange
            var client = ServerFactory.CreateDefaultClient();
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
            var client = ServerFactory.CreateDefaultClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            await ResponseAssert.IsHtmlDocumentAsync(response);
        }
    }
}
