// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using BasicWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.WebEncoders;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class TagHelpersTest
    {
        private const string SiteName = nameof(TagHelpersWebSite);

        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private static readonly Assembly _resourcesAssembly = typeof(TagHelpersTest).GetTypeInfo().Assembly;

        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Theory]
        [InlineData("Index")]
        [InlineData("About")]
        [InlineData("Help")]
        [InlineData("UnboundDynamicAttributes")]
        public async Task CanRenderViewsWithTagHelpers(string action)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/TagHelpersWebSite.Home." + action + ".html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/Home/" + action);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task CanRenderViewsWithTagHelpersAndUnboundDynamicAttributes_Encoded()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, services =>
            {
                _configureServices(services);
                services.AddTransient<IHtmlEncoder, TestHtmlEncoder>();
            });
            var client = server.CreateClient();
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/TagHelpersWebSite.Home.UnboundDynamicAttributes.Encoded.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await client.GetAsync("http://localhost/Home/UnboundDynamicAttributes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        public static TheoryData TagHelpersAreInheritedFromViewImportsPagesData
        {
            get
            {
                // action, expected
                return new TheoryData<string, string>
                {
                    {
                        "NestedViewImportsTagHelper",
                        @"<root>root-content</root>


<nested>nested-content</nested>"
                    },
                    {
                        "ViewWithLayoutAndNestedTagHelper",
                        @"layout:<root>root-content</root>


<nested>nested-content</nested>"
                    },
                    {
                        "ViewWithInheritedRemoveTagHelper",
                        @"layout:<root>root-content</root>


page:<root/>
<nested>nested-content</nested>"
                    },
                    {
                        "ViewWithInheritedTagHelperPrefix",
                        @"layout:<root>root-content</root>


page:<root>root-content</root>"
                    },
                    {
                        "ViewWithOverriddenTagHelperPrefix",
                        @"layout:<root>root-content</root>



page:<root>root-content</root>"
                    },
                    {
                        "ViewWithNestedInheritedTagHelperPrefix",
                        @"layout:<root>root-content</root>


page:<root>root-content</root>"
                    },
                    {
                        "ViewWithNestedOverriddenTagHelperPrefix",
                        @"layout:<root>root-content</root>



page:<root>root-content</root>"
                    },
                };
            }
        }

        [Theory]
        [MemberData(nameof(TagHelpersAreInheritedFromViewImportsPagesData))]
        public async Task TagHelpersAreInheritedFromViewImportsPages(string action, string expected)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var result = await client.GetStringAsync("http://localhost/Home/" + action);

            // Assert
            Assert.Equal(expected, result.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task ViewsWithModelMetadataAttributes_CanRenderForm()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/TagHelpersWebSite.Employee.Create.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await client.GetAsync("http://localhost/Employee/Create");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(
                PlatformNormalizer.NormalizeContent(expectedContent),
                responseContent,
                ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task ViewsWithModelMetadataAttributes_CanRenderPostedValue()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/TagHelpersWebSite.Employee.Details.AfterCreate.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);
            var validPostValues = new Dictionary<string, string>
            {
                { "FullName", "Boo" },
                { "Gender", "M" },
                { "Age", "22" },
                { "EmployeeId", "0" },
                { "JoinDate", "2014-12-01" },
                { "Email", "a@b.com" },
            };
            var postContent = new FormUrlEncodedContent(validPostValues);

            // Act
            var response = await client.PostAsync("http://localhost/Employee/Create", postContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task ViewsWithModelMetadataAttributes_CanHandleInvalidData()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();
            var outputFile = "compiler/resources/TagHelpersWebSite.Employee.Create.Invalid.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);
            var validPostValues = new Dictionary<string, string>
            {
                { "FullName", "Boo" },
                { "Gender", "M" },
                { "Age", "1000" },
                { "EmployeeId", "0" },
                { "Email", "a@b.com" },
                { "Salary", "z" },
            };
            var postContent = new FormUrlEncodedContent(validPostValues);

            // Act
            var response = await client.PostAsync("http://localhost/Employee/Create", postContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            // Mono issue - https://github.com/aspnet/External/issues/19
            Assert.Equal(
                PlatformNormalizer.NormalizeContent(expectedContent),
                responseContent,
                ignoreLineEndingDifferences: true);
#endif
        }
    }
}