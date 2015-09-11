// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class DirectivesTest : IClassFixture<MvcTestFixture<RazorWebSite.Startup>>
    {
        public DirectivesTest(MvcTestFixture<RazorWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ViewsInheritsUsingsAndInjectDirectivesFromViewStarts()
        {
            // Arrange
            var expected = @"Hello Person1";

            // Act
            var body = await Client.GetStringAsync(
                "http://localhost/Directives/ViewInheritsInjectAndUsingsFromViewImports");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewInheritsBasePageFromViewStarts()
        {
            // Arrange
            var expected = @"WriteLiteral says:layout:Write says:Write says:Hello Person2";

            // Act
            var body = await Client.GetStringAsync("http://localhost/Directives/ViewInheritsBasePageFromViewImports");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}