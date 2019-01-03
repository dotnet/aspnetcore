// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.using Microsoft.AspNetCore.Authorization;

using System.Net;
using System.Threading.Tasks;
using AzureAD.WebSite;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.AzureAD.FunctionalTests
{
    public class WebAuthenticationTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        public WebAuthenticationTests(WebApplicationFactory<Startup> fixture)
        {
            Factory = fixture;
        }

        public WebApplicationFactory<Startup> Factory { get; }

        public static TheoryData<string> NotAddedEndpoints =>
            new TheoryData<string>()
            {
                "/AzureAD/Account/AccessDenied",
                "/AzureAD/Account/Error",
                "/AzureAD/Account/SignedOut",
                "/AzureAD/Account/SignIn",
                "/AzureAD/Account/SignOut",
                "/AzureADB2C/Account/AccessDenied",
                "/AzureADB2C/Account/Error",
                "/AzureADB2C/Account/SignedOut",
                "/AzureADB2C/Account/SignIn",
                "/AzureADB2C/Account/ResetPassword",
                "/AzureADB2C/Account/EditProfile",
                "/AzureADB2C/Account/SignOut",
            };

        [Theory]
        [MemberData(nameof(NotAddedEndpoints))]
        public async Task Endpoints_NotAvailable_When_Authentication_NotAdded(string endpoint)
        {
            // Act & Assert
            var response = await Factory.CreateDefaultClient().GetAsync(endpoint);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        public static TheoryData<string, HttpStatusCode> AddedEndpointsStatusCodesAD =>
            new TheoryData<string, HttpStatusCode>()
            {
                { "/AzureAD/Account/AccessDenied", HttpStatusCode.OK },
                { "/AzureAD/Account/Error", HttpStatusCode.OK },
                { "/AzureAD/Account/SignedOut", HttpStatusCode.OK },
                { "/AzureAD/Account/SignIn", HttpStatusCode.Redirect },
                { "/AzureAD/Account/SignOut", HttpStatusCode.Redirect },
            };

        [Theory]
        [MemberData(nameof(AddedEndpointsStatusCodesAD))]
        public async Task ADEndpoints_AreAvailable_When_Authentication_IsAdded(string endpoint, HttpStatusCode expectedStatusCode)
        {
            // Act & Assert
            var client = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
                services =>
                {
                    services
                        .AddAuthentication(AzureADDefaults.AuthenticationScheme)
                        .AddAzureAD(o =>
                        {
                            o.Instance = "https://login.microsoftonline.com/";
                            o.Domain = "test.onmicrosoft.com";
                            o.ClientId = "ClientId";
                            o.TenantId = "TenantId";
                        });

                    services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, o =>
                    {
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            Issuer = "https://www.example.com",
                            TokenEndpoint = "https://www.example.com/token",
                            AuthorizationEndpoint = "https://www.example.com/authorize",
                            EndSessionEndpoint = "https://www.example.com/logout"
                        };
                    });

                    services.AddMvc(o => o.Filters.Add(
                        new AuthorizeFilter(new AuthorizationPolicyBuilder(new[] { AzureADDefaults.AuthenticationScheme })
                            .RequireAuthenticatedUser().Build())));
                })).CreateDefaultClient();

            var response = await client.GetAsync(endpoint);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }

        public static TheoryData<string, HttpStatusCode> AddedEndpointsStatusCodesADB2C =>
            new TheoryData<string, HttpStatusCode>()
            {
                { "/AzureADB2C/Account/AccessDenied", HttpStatusCode.OK },
                { "/AzureADB2C/Account/Error", HttpStatusCode.OK },
                { "/AzureADB2C/Account/SignedOut", HttpStatusCode.OK },
                { "/AzureADB2C/Account/SignIn", HttpStatusCode.Redirect },
                { "/AzureADB2C/Account/ResetPassword", HttpStatusCode.Redirect },
                { "/AzureADB2C/Account/EditProfile", HttpStatusCode.Redirect },
                { "/AzureADB2C/Account/SignOut", HttpStatusCode.Redirect }
            };

        [Theory]
        [MemberData(nameof(AddedEndpointsStatusCodesADB2C))]
        public async Task ADB2CEndpoints_AreAvailable_When_Authentication_IsAdded(string endpoint, HttpStatusCode expectedStatusCode)
        {
            // Act & Assert
            var client = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
                services =>
                {
                    services
                        .AddAuthentication(AzureADB2CDefaults.AuthenticationScheme)
                        .AddAzureADB2C(o =>
                        {
                            o.Instance = "https://login.microsoftonline.com/tfp/";
                            o.ClientId = "ClientId";
                            o.CallbackPath = "/signin-oidc";
                            o.Domain = "test.onmicrosoft.com";
                            o.SignUpSignInPolicyId = "B2C_1_SiUpIn";
                            o.ResetPasswordPolicyId = "B2C_1_SSPR";
                            o.EditProfilePolicyId = "B2C_1_SiPe";
                        });

                    services.Configure<OpenIdConnectOptions>(AzureADB2CDefaults.OpenIdScheme, o =>
                    {
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            Issuer = "https://www.example.com",
                            TokenEndpoint = "https://www.example.com/token",
                            AuthorizationEndpoint = "https://www.example.com/authorize",
                            EndSessionEndpoint = "https://www.example.com/logout"
                        };
                    });

                    services.AddMvc(o => o.Filters.Add(
                        new AuthorizeFilter(new AuthorizationPolicyBuilder(new[] { AzureADB2CDefaults.AuthenticationScheme })
                            .RequireAuthenticatedUser().Build())));
                })).CreateDefaultClient();

            var response = await client.GetAsync(endpoint);

            // Assert
            Assert.Equal(expectedStatusCode, response.StatusCode);
        }
    }
}
