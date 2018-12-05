// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class NoIdentityAddedTests : IClassFixture<ServerFactory<NoIdentityStartup, IdentityDbContext>>
    {
        public NoIdentityAddedTests(ServerFactory<NoIdentityStartup, IdentityDbContext> serverFactory)
        {
            ServerFactory = serverFactory;
        }

        public ServerFactory<NoIdentityStartup, IdentityDbContext> ServerFactory { get; }

        [Theory]
        [MemberData(nameof(IdentityEndpoints))]
        public async Task QueryingIdentityEndpointsReturnsNotFoundWhenIdentityIsNotPresent(string endpoint)
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) { return; };

            var client = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                .CreateClient();

            // Act
            var response = await client.GetAsync(endpoint);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        public static TheoryData<string> IdentityEndpoints => new TheoryData<string>
        {
            "/Identity/Account/AccessDenied",
            "/Identity/Account/ConfirmEmail",
            "/Identity/Account/ExternalLogin",
            "/Identity/Account/ForgotPassword",
            "/Identity/Account/ForgotPasswordConfirmation",
            "/Identity/Account/Lockout",
            "/Identity/Account/Login",
            "/Identity/Account/LoginWith2fa",
            "/Identity/Account/LoginWithRecoveryCode",
            "/Identity/Account/Logout",
            "/Identity/Account/Register",
            "/Identity/Account/ResetPassword",
            "/Identity/Account/ResetPasswordConfirmation",
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
    }
}
