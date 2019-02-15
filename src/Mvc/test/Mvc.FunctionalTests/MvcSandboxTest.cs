// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class MvcSandboxTest : IClassFixture<MvcTestFixture<MvcSandbox.Startup>>
    {
        public MvcSandboxTest(MvcTestFixture<MvcSandbox.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task Home_Pages_ReturnSuccess()
        {
            // Arrange & Act
            var response = await Client.GetStringAsync("http://localhost");

            // Assert
            Assert.Contains("This sandbox should give you a quick view of a basic MVC application.", response);
        }
        [Fact]
        public async Task RazorPages_ReturnSuccess()
        {
            // Arrange & Act
            var response = await Client.GetStringAsync("http://localhost/PagesHome");

            // Assert
            Assert.Contains("This file should give you a quick view of a Mvc Razor Page in action.", response);
        }
    }
}