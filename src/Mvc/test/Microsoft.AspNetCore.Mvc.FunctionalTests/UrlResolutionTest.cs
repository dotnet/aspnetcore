// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class UrlResolutionTest :
        IClassFixture<MvcTestFixture<RazorWebSite.Startup>>,
        IClassFixture<MvcEncodedTestFixture<RazorWebSite.Startup>>
    {
        private static readonly Assembly _resourcesAssembly = typeof(UrlResolutionTest).GetTypeInfo().Assembly;

        public UrlResolutionTest(
            MvcTestFixture<RazorWebSite.Startup> fixture,
            MvcEncodedTestFixture<RazorWebSite.Startup> encodedFixture)
        {
            Client = fixture.CreateDefaultClient();
            EncodedClient = encodedFixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        public HttpClient EncodedClient { get; }

        [Fact]
        public async Task AppRelativeUrlsAreResolvedCorrectly()
        {
            // Arrange
            var outputFile = "compiler/resources/RazorWebSite.UrlResolution.Index.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("http://localhost/UrlResolution/Index");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            responseContent = responseContent.Trim();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent.Trim(), responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task AppRelativeUrlsAreResolvedAndEncodedCorrectly()
        {
            // Arrange
            var outputFile = "compiler/resources/RazorWebSite.UrlResolution.Index.Encoded.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await EncodedClient.GetAsync("http://localhost/UrlResolution/Index");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            responseContent = responseContent.Trim();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent.Trim(), responseContent, ignoreLineEndingDifferences: true);
#endif
        }
    }
}