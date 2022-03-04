// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    /// <summary>
    /// These tests are for scenarios when <see cref="MvcOptions.RespectBrowserAcceptHeader"/> is <c>True</c>(default is False).
    /// </summary>
    public class RespectBrowserAcceptHeaderTests : IClassFixture<MvcTestFixture<FormatterWebSite.StartupWithRespectBrowserAcceptHeader>>
    {
        public RespectBrowserAcceptHeaderTests(MvcTestFixture<FormatterWebSite.StartupWithRespectBrowserAcceptHeader> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<FormatterWebSite.StartupWithRespectBrowserAcceptHeader>();

        public HttpClient Client { get; }

        [Fact]
        public async Task ReturnStringFromAction_StringOutputFormatterDoesNotWriteTheResponse()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "RespectBrowserAcceptHeader/ReturnString");
            request.Headers.Accept.ParseAdd("text/html, application/json, image/jpeg, */*; q=.2");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal("\"Hello World!\"", responseData);
        }

        [Fact]
        public async Task ReturnStringFromAction_AcceptHeaderWithTextPlain_WritesTextPlainResponse()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "RespectBrowserAcceptHeader/ReturnString");
            request.Headers.Accept.ParseAdd("text/plain; charset=utf-8");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(response.Content);
            Assert.NotNull(response.Content.Headers.ContentType);
            Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType.ToString());
            var responseData = await response.Content.ReadAsStringAsync();
            Assert.Equal("Hello World!", responseData);
        }
    }
}