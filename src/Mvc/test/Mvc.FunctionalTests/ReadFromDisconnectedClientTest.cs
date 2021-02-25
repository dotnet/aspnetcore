// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    // These tests verify the behavior of MVC when responding to a client that simulates a disconnect.
    // See https://github.com/dotnet/aspnetcore/issues/13333
    public class ReadFromDisconnectedClientTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWhereReadingRequestBodyThrows>>
    {
        public ReadFromDisconnectedClientTest(MvcTestFixture<BasicWebSite.StartupWhereReadingRequestBodyThrows> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<BasicWebSite.StartupWhereReadingRequestBodyThrows>();

        public HttpClient Client { get; }

        [Fact]
        public async Task ActionWithAntiforgery_Returns400_WhenReadingBodyThrows()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "ReadFromThrowingRequestBody/AppliesAntiforgeryValidation");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task ActionReadingForm_ReturnsInvalidModelState_WhenReadingBodyThrows()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Post, "ReadFromThrowingRequestBody/ReadForm");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["key"] = "value",
            });

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.BadRequest);
            var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            var error = Assert.Single(problem.Errors);
            Assert.Empty(error.Key);
        }
    }
}
