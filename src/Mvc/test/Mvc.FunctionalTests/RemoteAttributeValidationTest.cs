// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class RemoteAttributeValidationTest : IClassFixture<MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting>>
    {
        private static readonly Assembly _resourcesAssembly =
            typeof(RemoteAttributeValidationTest).GetTypeInfo().Assembly;

        public RemoteAttributeValidationTest(MvcTestFixture<BasicWebSite.StartupWithoutEndpointRouting> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("Area1", "/Area1")]
        [InlineData("Root", "")]
        public async Task RemoteAttribute_LeadsToExpectedValidationAttributes(string areaName, string pathSegment)
        {
            // Arrange
            var outputFile = "compiler/resources/BasicWebSite." + areaName + ".RemoteAttribute_Home.Create.html";
            var expectedContent =
                await ResourceFile.ReadResourceAsync(_resourcesAssembly, outputFile, sourceFile: false);
            var url = "http://localhost" + pathSegment + "/RemoteAttribute_Home/Create";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);

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
        [InlineData("", "\"/RemoteAttribute_Verify/IsIdAvailable rejects UserId1: 'Joe1'.\"")]
        [InlineData("/Area1", "false")]
        [InlineData("/Area2",
            "\"/Area2/RemoteAttribute_Verify/IsIdAvailable rejects 'Joe4' with 'Joe1', 'Joe2', and 'Joe3'.\"")]
        public async Task RemoteAttribute_VerificationAction_GetReturnsExpectedJson(
            string pathSegment,
            string expectedContent)
        {
            // Arrange
            var url = "http://localhost" + pathSegment +
                "/RemoteAttribute_Verify/IsIdAvailable?UserId1=Joe1&UserId2=Joe2&UserId3=Joe3&UserId4=Joe4";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, responseContent);
        }

        [Theory]
        [InlineData("", "\"/RemoteAttribute_Verify/IsIdAvailable rejects UserId1: 'Jane1'.\"")]
        [InlineData("/Area1", "false")]
        public async Task RemoteAttribute_VerificationAction_PostReturnsExpectedJson(
            string pathSegment,
            string expectedContent)
        {
            // Arrange
            var url = "http://localhost" + pathSegment + "/RemoteAttribute_Verify/IsIdAvailable";
            var contentDictionary = new Dictionary<string, string>
            {
                { "UserId1", "Jane1" },
                { "UserId2", "Jane2" },
                { "UserId3", "Jane3" },
                { "UserId4", "Jane4" },
            };
            var content = new FormUrlEncodedContent(contentDictionary);

            // Act
            var response = await Client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, responseContent);
        }
    }
}