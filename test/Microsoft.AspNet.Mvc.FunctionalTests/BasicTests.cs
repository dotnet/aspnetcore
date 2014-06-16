// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Threading.Tasks;
using BasicWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class BasicTests
    {
        private readonly IServiceProvider _provider;
        private readonly Action<IBuilder> _app = new Startup().Configure;

        // Some tests require comparing the actual response body against an expected response baseline
        // so they require a reference to the assembly on which the resources are located, in order to
        // make the tests less verbose, we get a reference to the assembly with the resources and we
        // use it on all the rest of the tests.
        private readonly Assembly _resourcesAssembly = typeof(BasicTests).GetTypeInfo().Assembly;

        public BasicTests()
        {
            _provider = TestHelper.CreateServices("BasicWebSite");
        }

        [InlineData("http://localhost/")]
        [InlineData("http://localhost/Home")]
        [InlineData("http://localhost/Home/Index")]
        [InlineData("http://localhost/Users")]
        [InlineData("http://localhost/Monitor/CountActionDescriptorInvocations")]
        public async Task CanRender_ViewsWithLayout(string url)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;

            // The K runtime compiles every file under compiler/resources as a resource at runtime with the same name
            // as the file name, in order to update a baseline you just need to change the file in that folder.
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync("BasicWebSite.Home.Index.html");

            // Act

            // The host is not important as everything runs in memory and tests are isolated from each other.
            var result = await client.GetAsync(url);
            var responseContent = await result.ReadBodyAsStringAsync();

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(result.ContentType, "text/html; charset=utf-8");
            Assert.Equal(expectedContent, responseContent);
        }

        [Fact]
        public async Task CanRender_SimpleViews()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContent = await _resourcesAssembly.ReadResourceAsStringAsync("BasicWebSite.Home.PlainView.html");

            // Act
            var result = await client.GetAsync("http://localhost/Home/PlainView");
            var responseContent = await result.ReadBodyAsStringAsync();

            // Assert
            Assert.Equal(200, result.StatusCode);
            Assert.Equal(result.ContentType, "text/html; charset=utf-8");
            Assert.Equal(expectedContent, responseContent);
        }

        [Fact]
        public async Task CanReturn_ResultsWithoutContent()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;

            // Act
            var result = await client.GetAsync("http://localhost/Home/NoContentResult");

            // Assert
            Assert.Equal(204, result.StatusCode);
            Assert.Null(result.ContentType);
            Assert.Null(result.ContentLength);
            Assert.NotNull(result.Body);
            Assert.Equal(0, result.Body.Length);
        }

        [Fact]
        public async Task ReturningTaskFromAction_ProducesNoContentResult()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;

            // Act
            var result = await client.GetAsync("http://localhost/Home/ActionReturningTask");

            // Assert
            Assert.Equal(204, result.StatusCode);
            var body = await result.ReadBodyAsStringAsync();
            Assert.Equal("Hello world", body);
        }

        [Fact]
        public async Task ActionDescriptors_CreatedOncePerRequest()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;

            var expectedContent = "1";

            // Call the server 3 times, and make sure the return value remains the same.
            var results = new string[3];

            // Act
            for (int i = 0; i < 3; i++)
            {
                var result = await client.GetAsync("http://localhost/Monitor/CountActionDescriptorInvocations");
                Assert.Equal(200, result.StatusCode);
                results[i] = await result.ReadBodyAsStringAsync();
            }

            // Assert
            Assert.Equal(expectedContent, results[0]);
            Assert.Equal(expectedContent, results[1]);
            Assert.Equal(expectedContent, results[2]);
        }
    }
}