// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using Grpc.AspNetCore.Server;
using Grpc.Core;
using Grpc.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MethodOptions = Grpc.Shared.Server.MethodOptions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class JsonTranscodingServerCallContextTests
{
    [Fact]
    public void CancellationToken_Get_MatchHttpContextRequestAborted()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var httpContext = CreateHttpContext(cancellationToken: cts.Token);
        var serverCallContext = CreateServerCallContext(httpContext);

        // Act
        var ct = serverCallContext.CancellationToken;

        // Assert
        Assert.Equal(cts.Token, ct);
    }

    [Fact]
    public void RequestHeaders_Get_PopulatedFromHttpContext()
    {
        // Arrange
        var httpContext = CreateHttpContext();
        httpContext.Request.Headers.Append("TestName", "TestValue");
        httpContext.Request.Headers.Append(":method", "GET");
        httpContext.Request.Headers.Append("grpc-encoding", "identity");
        httpContext.Request.Headers.Append("grpc-timeout", "1S");
        httpContext.Request.Headers.Append("hello-bin", Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        var serverCallContext = CreateServerCallContext(httpContext);

        // Act
        var headers = serverCallContext.RequestHeaders;

        // Assert
        Assert.Equal(2, headers.Count);
        Assert.Equal("testname", headers[0].Key);
        Assert.Equal("TestValue", headers[0].Value);
        Assert.Equal("hello-bin", headers[1].Key);
        Assert.True(headers[1].IsBinary);
        Assert.Equal(new byte[] { 1, 2, 3 }, headers[1].ValueBytes);
    }

    private static DefaultHttpContext CreateHttpContext(CancellationToken cancellationToken = default)
    {
        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("localhost");
        httpContext.RequestServices = serviceProvider;
        httpContext.Response.Body = new MemoryStream();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Features.Set<IHttpRequestLifetimeFeature>(new HttpRequestLifetimeFeature(cancellationToken));
        return httpContext;
    }

    private class HttpRequestLifetimeFeature : IHttpRequestLifetimeFeature
    {
        public HttpRequestLifetimeFeature(CancellationToken cancellationToken)
        {
            RequestAborted = cancellationToken;
        }

        public CancellationToken RequestAborted { get; set; }

        public void Abort()
        {
        }
    }

    private static JsonTranscodingServerCallContext CreateServerCallContext(DefaultHttpContext httpContext)
    {
        return new JsonTranscodingServerCallContext(
            httpContext,
            MethodOptions.Create(Enumerable.Empty<GrpcServiceOptions>()),
            new Method<object, object>(
                MethodType.Unary,
                "Server",
                "Method",
                new Marshaller<object>(o => null!, c => null!),
                new Marshaller<object>(o => null!, c => null!)),
            new CallHandlerDescriptorInfo(
                null,
                null,
                false,
                null,
                new Dictionary<string, RouteParameter>(),
                JsonTranscodingRouteAdapter.Parse(HttpRoutePattern.Parse("/")!)),
            NullLogger.Instance);
    }
}
