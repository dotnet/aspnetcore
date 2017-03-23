// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests
{
    public class WelcomePageSampleTest : IClassFixture<TestFixture<WelcomePageSample.Startup>>
    {
        public WelcomePageSampleTest(TestFixture<WelcomePageSample.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task WelcomePage_ShowsWelcome()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");

            var response = await Client.SendAsync(request);

            var bytes = await response.Content.ReadAsByteArrayAsync();
            Assert.True(bytes.Length > 1);
            Assert.NotEqual(0xEF, bytes[0]); // No leading UTF-8 BOM

            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Your ASP.NET Core application has been successfully started", body);
        }
    }
}
