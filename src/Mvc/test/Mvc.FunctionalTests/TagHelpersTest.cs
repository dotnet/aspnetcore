// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class TagHelpersTest :
        IClassFixture<MvcTestFixture<TagHelpersWebSite.Startup>>,
        IClassFixture<MvcEncodedTestFixture<TagHelpersWebSite.Startup>>
    {
        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private static readonly Assembly _resourcesAssembly = typeof(TagHelpersTest).GetTypeInfo().Assembly;

        public TagHelpersTest(
            MvcTestFixture<TagHelpersWebSite.Startup> fixture,
            MvcEncodedTestFixture<TagHelpersWebSite.Startup> encodedFixture)
        {
            Client = fixture.CreateDefaultClient();
            EncodedClient = encodedFixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        public HttpClient EncodedClient { get; }

        [Theory]
        [InlineData("GlobbingTagHelpers")]
        [InlineData("Index")]
        [InlineData("About")]
        [InlineData("Help")]
        [InlineData("UnboundDynamicAttributes")]
        [InlineData("ViewComponentTagHelpers")]
        public async Task CanRenderViewsWithTagHelpers(string action)
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/TagHelpersWebSite.Home." + action + ".html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await Client.GetAsync("http://localhost/Home/" + action);

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
        public async Task GivesCorrectCallstackForSyncronousCalls()
        {
            // Regression test for https://github.com/dotnet/aspnetcore/issues/15367
            // Arrange
            var exception = await Assert.ThrowsAsync<HttpRequestException>(async () => await Client.GetAsync("http://localhost/Home/MyHtml"));

            // Assert
            Assert.Equal("Should be visible", exception.InnerException.InnerException.Message);
        }

        [Fact]
        public async Task CanRenderViewsWithTagHelpersAndUnboundDynamicAttributes_Encoded()
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/TagHelpersWebSite.Home.UnboundDynamicAttributes.Encoded.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            // The host is not important as everything runs in memory and tests are isolated from each other.
            var response = await EncodedClient.GetAsync("http://localhost/Home/UnboundDynamicAttributes");

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
        public async Task ReRegisteringAntiforgeryTokenInsideFormTagHelper_DoesNotAddDuplicateAntiforgeryTokenFields()
        {
            // Arrange
            var expectedMediaType = MediaTypeHeaderValue.Parse("text/html; charset=utf-8");
            var outputFile = "compiler/resources/TagHelpersWebSite.Employee.DuplicateAntiforgeryTokenRegistration.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("http://localhost/Employee/DuplicateAntiforgeryTokenRegistration");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedMediaType, response.Content.Headers.ContentType);

            responseContent = responseContent.Trim();

            var forgeryToken = AntiforgeryTestHelper.RetrieveAntiforgeryToken(
                responseContent, "/Employee/DuplicateAntiforgeryTokenRegistration");

#if GENERATE_BASELINES
            // Reverse usual substitution and insert a format item into the new file content.
            responseContent = responseContent.Replace(forgeryToken, "{0}");
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            expectedContent = string.Format(expectedContent, forgeryToken);
            Assert.Equal(
                expectedContent.Trim(),
                responseContent,
                ignoreLineEndingDifferences: true);
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
            // Arrange & Act
            var result = await Client.GetStringAsync("http://localhost/Home/" + action);

            // Assert
            Assert.Equal(expected, result.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task DefaultInheritedTagsCanBeRemoved()
        {
            // Arrange
            var expected =
@"<a href=""~/VirtualPath"">Virtual path</a>";

            var result = await Client.GetStringAsync("RemoveDefaultInheritedTagHelpers");

            // Assert
            Assert.Equal(expected, result.Trim(), ignoreLineEndingDifferences: true);
        }

        [Fact]
        public async Task ViewsWithModelMetadataAttributes_CanRenderForm()
        {
            // Arrange
            var outputFile = "compiler/resources/TagHelpersWebSite.Employee.Create.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync("http://localhost/Employee/Create");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(
                expectedContent,
                responseContent,
                ignoreLineEndingDifferences: true);
#endif
        }

        [Fact]
        public async Task ViewsWithModelMetadataAttributes_CanRenderPostedValue()
        {
            // Arrange
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
            var response = await Client.PostAsync("http://localhost/Employee/Create", postContent);

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
            var response = await Client.PostAsync("http://localhost/Employee/Create", postContent);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var responseContent = await response.Content.ReadAsStringAsync();
#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(
                expectedContent,
                responseContent,
                ignoreLineEndingDifferences: true);
#endif
        }

        [Theory]
        [InlineData("Index")]
        [InlineData("CustomEncoder")]
        [InlineData("NullEncoder")]
        [InlineData("TwoEncoders")]
        [InlineData("ThreeEncoders")]
        public async Task EncodersPages_ReturnExpectedContent(string actionName)
        {
            // Arrange
            var outputFile = $"compiler/resources/TagHelpersWebSite.Encoders.{ actionName }.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);

            // Act
            var response = await Client.GetAsync($"/Encoders/{ actionName }");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var responseContent = await response.Content.ReadAsStringAsync();

#if GENERATE_BASELINES
            ResourceFile.UpdateFile(_resourcesAssembly, outputFile, expectedContent, responseContent);
#else
            Assert.Equal(expectedContent, responseContent, ignoreLineEndingDifferences: true);
#endif
        }
    }
}
