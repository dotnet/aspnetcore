// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server
{
    public class CircuitDisconnectMiddlewareTest
    {
        [Theory]
        [InlineData("GET")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("HEAD")]
        public async Task DisconnectMiddleware_OnlyAccepts_PostRequests(string httpMethod)
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var registry = new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                circuitIdFactory);

            var middleware = new CircuitDisconnectMiddleware(
                NullLogger<CircuitDisconnectMiddleware>.Instance,
                registry,
                circuitIdFactory,
                (ctx) => Task.CompletedTask);

            var context = new DefaultHttpContext();
            context.Request.Method = httpMethod;

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(StatusCodes.Status405MethodNotAllowed, context.Response.StatusCode);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("application/json")]
        public async Task Returns400BadRequest_ForInvalidContentTypes(string contentType)
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var registry = new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                circuitIdFactory);

            var middleware = new CircuitDisconnectMiddleware(
                NullLogger<CircuitDisconnectMiddleware>.Instance,
                registry,
                circuitIdFactory,
                (ctx) => Task.CompletedTask);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = contentType;

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task Returns400BadRequest_IfNoCircuitIdOnForm()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var registry = new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                circuitIdFactory);

            var middleware = new CircuitDisconnectMiddleware(
                NullLogger<CircuitDisconnectMiddleware>.Instance,
                registry,
                circuitIdFactory,
                (ctx) => Task.CompletedTask);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/x-www-form-urlencoded";

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task Returns400BadRequest_InvalidCircuitId()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var registry = new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                circuitIdFactory);

            var middleware = new CircuitDisconnectMiddleware(
                NullLogger<CircuitDisconnectMiddleware>.Instance,
                registry,
                circuitIdFactory,
                (ctx) => Task.CompletedTask);

            using var memory = new MemoryStream();
            await new FormUrlEncodedContent(new Dictionary<string, string> { ["circuitId"] = "1234" }).CopyToAsync(memory);
            memory.Seek(0, SeekOrigin.Begin);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.Body = memory;

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        }

        [Fact]
        public async Task Returns200OK_NonExistingCircuit()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var circuitId = circuitIdFactory.CreateCircuitId();
            var registry = new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                circuitIdFactory);

            var middleware = new CircuitDisconnectMiddleware(
                NullLogger<CircuitDisconnectMiddleware>.Instance,
                registry,
                circuitIdFactory,
                (ctx) => Task.CompletedTask);

            using var memory = new MemoryStream();
            await new FormUrlEncodedContent(new Dictionary<string, string> { ["circuitId"] = circuitId.Secret, }).CopyToAsync(memory);
            memory.Seek(0, SeekOrigin.Begin);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.Body = memory;

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task GracefullyTerminates_ConnectedCircuit()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var circuitId = circuitIdFactory.CreateCircuitId();
            var testCircuitHost = TestCircuitHost.Create(circuitId);

            var registry = new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                circuitIdFactory);

            registry.Register(testCircuitHost);

            var middleware = new CircuitDisconnectMiddleware(
                NullLogger<CircuitDisconnectMiddleware>.Instance,
                registry,
                circuitIdFactory,
                (ctx) => Task.CompletedTask);

            using var memory = new MemoryStream();
            await new FormUrlEncodedContent(new Dictionary<string, string> { ["circuitId"] = circuitId.Secret, }).CopyToAsync(memory);
            memory.Seek(0, SeekOrigin.Begin);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.Body = memory;

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public async Task GracefullyTerminates_DisconnectedCircuit()
        {
            // Arrange
            var circuitIdFactory = TestCircuitIdFactory.CreateTestFactory();
            var circuitId = circuitIdFactory.CreateCircuitId();
            var circuitHost = TestCircuitHost.Create(circuitId);

            var registry = new CircuitRegistry(
                Options.Create(new CircuitOptions()),
                NullLogger<CircuitRegistry>.Instance,
                circuitIdFactory);

            registry.Register(circuitHost);
            await registry.DisconnectAsync(circuitHost, "1234");

            var middleware = new CircuitDisconnectMiddleware(
                NullLogger<CircuitDisconnectMiddleware>.Instance,
                registry,
                circuitIdFactory,
                (ctx) => Task.CompletedTask);

            using var memory = new MemoryStream();
            await new FormUrlEncodedContent(new Dictionary<string, string> { ["circuitId"] = circuitId.Secret }).CopyToAsync(memory);
            memory.Seek(0, SeekOrigin.Begin);

            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Post;
            context.Request.ContentType = "application/x-www-form-urlencoded";
            context.Request.Body = memory;

            // Act
            await middleware.Invoke(context);

            // Assert
            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }
    }
}
