// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;
using AzureAD.WebSite;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.AzureADB2C.UI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.AzureAD.FunctionalTests
{
    public class ApiAuthenticationTests : IClassFixture<WebApplicationFactory<Startup>>
    {
        public ApiAuthenticationTests(WebApplicationFactory<Startup> fixture)
        {
            Factory = fixture;
        }

        public WebApplicationFactory<Startup> Factory { get; }

        [Fact]
        public async Task BearerAzureAD_Challenges_UnauthorizedRequests()
        {
            // Arrange
            var client = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
                services =>
                {
                    services.AddAuthentication(AzureADDefaults.BearerAuthenticationScheme)
                        .AddAzureADBearer(o =>
                        {
                            o.Instance = "https://login.microsoftonline.com/";
                            o.Domain = "test.onmicrosoft.com";
                            o.ClientId = "ClientId";
                            o.TenantId = "TenantId";
                        });

                    services.Configure<JwtBearerOptions>(AzureADDefaults.JwtBearerAuthenticationScheme, o =>
                    {
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            Issuer = "https://www.example.com",
                        };
                    });
                })).CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/api/get");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task BearerAzureADB2C_Challenges_UnauthorizedRequests()
        {
            // Arrange
            var client = Factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(
                services =>
                {
                    services.AddAuthentication(AzureADB2CDefaults.BearerAuthenticationScheme)
                        .AddAzureADB2CBearer(o =>
                        {
                            o.Instance = "https://login.microsoftonline.com/";
                            o.Domain = "test.onmicrosoft.com";
                            o.ClientId = "ClientId";
                            o.SignUpSignInPolicyId = "B2c_1_SiSu";
                        });

                    services.Configure<JwtBearerOptions>(AzureADB2CDefaults.JwtBearerAuthenticationScheme, o =>
                    {
                        o.Configuration = new OpenIdConnectConfiguration()
                        {
                            Issuer = "https://www.example.com",
                        };
                    });
                })).CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/api/get");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}