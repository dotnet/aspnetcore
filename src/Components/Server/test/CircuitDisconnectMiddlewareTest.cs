// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Components.Server;

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
            circuitIdFactory,
            CreatePersistenceManager());

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
            circuitIdFactory,
            CreatePersistenceManager());

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
            circuitIdFactory,
            CreatePersistenceManager());

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
            circuitIdFactory,
            CreatePersistenceManager());

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
            circuitIdFactory,
            CreatePersistenceManager());

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
            circuitIdFactory,
            CreatePersistenceManager());

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
            circuitIdFactory,
            CreatePersistenceManager());

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

    private static CircuitPersistenceManager CreatePersistenceManager()
    {
        var circuitPersistenceManager = new CircuitPersistenceManager(
            Options.Create(new CircuitOptions()),
            new Endpoints.ServerComponentSerializer(new EphemeralDataProtectionProvider()),
            Mock.Of<ICircuitPersistenceProvider>(),
            new EphemeralDataProtectionProvider());
        return circuitPersistenceManager;
    }
}
