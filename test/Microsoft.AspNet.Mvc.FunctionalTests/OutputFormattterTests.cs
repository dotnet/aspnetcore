// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using ConnegWebsite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class OutputFormatterTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("ConnegWebsite");
        private readonly Action<IBuilder> _app = new Startup().Configure;

        [Theory]
        [InlineData("ReturnTaskOfString")]
        [InlineData("ReturnTaskOfObject_StringValue")]
        [InlineData("ReturnString")]
        [InlineData("ReturnObject_StringValue")]
        public async Task TextPlainFormatter_ForStringValues_GetsSelectedReturnsTextPlainContentType(string actionName)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "text/plain;charset=utf-8";
            var expectedBody = actionName;

            // Act
            var result = await client.GetAsync("http://localhost/TextPlain/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
        }

        [Theory]
        [InlineData("ReturnTaskOfObject_ObjectValue")]
        [InlineData("ReturnObject_ObjectValue")]
        public async Task JsonOutputFormatter_ForNonStringValue_GetsSelected(string actionName)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expectedContentType = "application/json;charset=utf-8";
            var expectedBody = actionName;

            // Act
            var result = await client.GetAsync("http://localhost/TextPlain/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
        }

        [Theory]
        [InlineData("ReturnTaskOfString_NullValue")]
        [InlineData("ReturnTaskOfObject_StringValue")]
        [InlineData("ReturnTaskOfObject_NullValue")]
        [InlineData("ReturnObject_NullValue")]
        public async Task NoContentFormatter_ForNullValue_GetsSelectedAndWritesResponse(string actionName)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            string expectedContentType = null;

            // ReadBodyAsString returns empty string instead of null.
            string expectedBody = "";

            // Act
            var result = await client.GetAsync("http://localhost/NoContent/" + actionName);

            // Assert
            Assert.Equal(expectedContentType, result.HttpContext.Response.ContentType);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expectedBody, body);
            Assert.Equal(204, result.HttpContext.Response.StatusCode);
            Assert.Equal(0, result.HttpContext.Response.ContentLength);
        }
    }
}