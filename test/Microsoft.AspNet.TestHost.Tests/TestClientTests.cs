// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;
using Microsoft.Net.Runtime;
using Xunit;

namespace Microsoft.AspNet.TestHost.Tests
{
    public class TestClientTests
    {
        private readonly IServiceProvider _services;
        private readonly TestServer _server;

        public TestClientTests()
        {
            _services = new ServiceCollection()
                .AddSingleton<IApplicationEnvironment, TestApplicationEnvironment>()
                .BuildServiceProvider();

            _server = TestServer.Create(_services, app => app.Run(async ctx => { }));
        }

        [Fact]
        public async Task SendAsync_ConfiguresRequestProperly()
        {
            // Arrange
            var client = _server.Handler;

            // Act
            var response = await client.SendAsync("GET", "http://localhost:12345/Home/Index?id=3&name=peter#fragment");
            var request = response.HttpContext.Request;

            // Assert
            Assert.NotNull(request);
            Assert.Equal("HTTP/1.1", request.Protocol);
            Assert.Equal("GET", request.Method);
            Assert.Equal("http", request.Scheme);
            Assert.Equal("localhost:12345", request.Host.Value);
            Assert.Equal("", request.PathBase.Value);
            Assert.True(request.Path.HasValue);
            Assert.Equal("/Home/Index", request.Path.Value);
            Assert.Equal("?id=3&name=peter", request.QueryString.Value);
            Assert.Null(request.ContentLength);
            Assert.Equal(1, request.Headers.Count);
            Assert.True(request.Headers.ContainsKey("Host"));
        }

        [Fact]
        public async Task SendAsync_InvokesCallbackWhenPassed()
        {
            // Arrange
            var client = _server.Handler;
            var invoked = false;

            // Act
            var response = await client.SendAsync("GET", "http://localhost:12345/", null, null, _ => invoked = true);

            // Assert
            Assert.True(invoked);
        }

        [Fact]
        public async Task SendAsync_RespectsExistingHost()
        {
            // Arrange
            var client = _server.Handler;
            var headers = new Dictionary<string, string[]> { { "Host", new string[] { "server:12345" } } };

            // Act
            var response = await client.SendAsync("GET", "http://localhost:12345/Home/", headers);
            var request = response.HttpContext.Request;

            // Assert
            Assert.Equal("server:12345", request.Host.Value);
        }

        [Fact]
        public async Task SendAsync_RespectsArgumentBody()
        {
            // Arrange
            var client = _server.Handler;
            var headers = new Dictionary<string, string[]> { { "Content-Type", new string[] { "text/plain" } } };
            var body = new MemoryStream();
            new StreamWriter(body).Write("Hello world");
            body.Position = 0;

            // Act
            var response = await client.SendAsync("POST", "http://host/", headers, body);
            var request = response.HttpContext.Request;

            // Assert
            Assert.Same(body, request.Body);
            Assert.Equal(0, request.Body.Position);
            Assert.Equal(body.Length, request.ContentLength);
        }

        [Fact]
        public async Task SendAsync_RewindsTheResponseStream()
        {
            // Arrange
            var server = TestServer.Create(_services, app => app.Run(ctx => ctx.Response.WriteAsync("Hello world")));
            var client = server.Handler;

            // Act
            var response = await client.SendAsync("GET", "http://localhost");

            // Assert
            Assert.Equal(0, response.Body.Position);
            Assert.Equal("Hello world", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public async Task PutAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
                await ctx.Response.WriteAsync(new StreamReader(ctx.Request.Body).ReadToEnd());
            var server = TestServer.Create(_services, app => app.Run(appDelegate));
            var client = server.Handler;

            // Act
            var response = await client.PutAsync("http://localhost:12345", "Hello world", "text/plain");
            var request = response.HttpContext.Request;

            // Assert
            Assert.Equal("PUT", request.Method);
            Assert.Equal("Hello world", new StreamReader(response.Body).ReadToEnd());
        }

        [Fact]
        public async Task PostAsyncWorks()
        {
            // Arrange
            RequestDelegate appDelegate = async ctx =>
                await ctx.Response.WriteAsync(new StreamReader(ctx.Request.Body).ReadToEnd());
            var server = TestServer.Create(_services, app => app.Run(appDelegate));
            var client = server.Handler;

            // Act
            var response = await client.PostAsync("http://localhost:12345", "Hello world", "text/plain");
            var request = response.HttpContext.Request;

            // Assert
            Assert.Equal("POST", request.Method);
            Assert.Equal("Hello world", new StreamReader(response.Body).ReadToEnd());
        }
    }
}
