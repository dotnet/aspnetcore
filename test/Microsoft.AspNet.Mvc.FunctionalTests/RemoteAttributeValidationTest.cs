// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RemoteAttributeValidationTest
    {
        private const string SiteName = nameof(ValidationWebSite);
        private static readonly Assembly _resourcesAssembly =
            typeof(RemoteAttributeValidationTest).GetTypeInfo().Assembly;
        private readonly Action<IApplicationBuilder> _app = new ValidationWebSite.Startup().Configure;

        [Theory]
        [InlineData("Aria", "/Aria")]
        [InlineData("Root", "")]
        public async Task RemoteAttribute_LeadsToExpectedValidationAttributes(string areaName, string pathSegment)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync(
                "compiler/resources/ValidationWebSite." + areaName + ".RemoteAttribute_Home.Create.html");
            var url = "http://localhost" + pathSegment + "/RemoteAttribute_Home/Create";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, responseContent);
        }

        [Theory]
        [InlineData("", "\"/RemoteAttribute_Verify/IsIdAvailable rejects Joe1.\"")]
        [InlineData("/Aria", "false")]
        [InlineData("/AnotherAria",
            "\"/AnotherAria/RemoteAttribute_Verify/IsIdAvailable rejects 'Joe4' with 'Joe1', 'Joe2', and 'Joe3'.\"")]
        public async Task RemoteAttribute_VerificationAction_GetReturnsExpectedJson(
            string pathSegment,
            string expectedContent)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            var url = "http://localhost" + pathSegment +
                "/RemoteAttribute_Verify/IsIdAvailable?UserId1=Joe1&UserId2=Joe2&UserId3=Joe3&UserId4=Joe4";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, responseContent);
        }

        [Theory]
        [InlineData("", "\"/RemoteAttribute_Verify/IsIdAvailable rejects Jane1.\"")]
        [InlineData("/Aria", "false")]
        public async Task RemoteAttribute_VerificationAction_PostReturnsExpectedJson(
            string pathSegment,
            string expectedContent)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
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
            var response = await client.PostAsync(url, content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", response.Content.Headers.ContentType.CharSet);
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedContent, responseContent);
        }
    }
}