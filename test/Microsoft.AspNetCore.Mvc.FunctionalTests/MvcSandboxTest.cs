// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class MvcSandboxTest : IClassFixture<MvcTestFixture<MvcSandbox.Startup>>
    {
        public MvcSandboxTest(MvcTestFixture<MvcSandbox.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("")]                        // Shared/MyView.cshtml
        [InlineData("/")]                       // Shared/MyView.cshtml
        [InlineData("/Home/Index")]             // Shared/MyView.cshtml
        public async Task Home_Pages_ReturnSuccess(string path)
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost" + path);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Home_NotFoundAction_Returns404()
        {
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/Home/NotFound");

            // Assert
            Assert.NotNull(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}