// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using LocalizationSample;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Localization.FunctionalTests
{
    public class LocalizationSampleTest
    {
        [Fact]
        public async Task LocalizationSampleSmokeTest()
        {
            // Arrange
            using var host = new HostBuilder()
                .ConfigureWebHost(webHostBuilder =>
                {
                    webHostBuilder
                    .UseTestServer()
                    .UseStartup(typeof(Startup));
                }).Build();

            await host.StartAsync();

            var testHost = host.GetTestServer();
            var locale = "fr-FR";
            var client = testHost.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "My/Resources");
            var cookieValue = $"c={locale}|uic={locale}";
            request.Headers.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookieValue}");

            // Act
            var response = await client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("<h1>Bonjour</h1>", await response.Content.ReadAsStringAsync());
        }
    }
}
