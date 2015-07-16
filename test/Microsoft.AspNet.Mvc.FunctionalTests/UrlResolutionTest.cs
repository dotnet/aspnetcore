// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class UrlResolutionTest
    {
        private const string SiteName = nameof(RazorWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;
        private static readonly Assembly _resourcesAssembly = typeof(UrlResolutionTest).GetTypeInfo().Assembly;

        [Fact]
        public async Task AppRelativeUrlsAreResolvedCorrectly()
        {
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);

            var client = server.CreateClient();
            var outputFile = "compiler/resources/RazorWebSite.UrlResolution.Index.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await client.GetAsync("http://localhost/UrlResolution/Index");
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
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                _configureServices(services);
                services.AddTransient<IHtmlEncoder, TestHtmlEncoder>();
            });
            var client = server.CreateClient();
            var outputFile = "compiler/resources/RazorWebSite.UrlResolution.Index.Encoded.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await client.GetAsync("http://localhost/UrlResolution/Index");
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