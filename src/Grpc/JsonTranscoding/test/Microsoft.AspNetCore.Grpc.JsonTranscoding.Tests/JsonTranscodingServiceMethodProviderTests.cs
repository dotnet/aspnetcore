// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Binding;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.TestObjects;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class JsonTranscodingServiceMethodProviderTests
{
    [Fact]
    public void AddMethod_OptionGet_ResolveMethod()
    {
        // Arrange & Act
        var endpoints = MapEndpoints<JsonTranscodingGreeterService>();

        // Assert
        var endpoint = FindGrpcEndpoint(endpoints, nameof(JsonTranscodingGreeterService.SayHello));

        Assert.Equal("GET", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());
        Assert.Equal("/v1/greeter/{name}", endpoint.RoutePattern.RawText);
        Assert.Single(endpoint.RoutePattern.Parameters);
        Assert.Equal("name", endpoint.RoutePattern.Parameters[0].Name);
    }

    [Fact]
    public void AddMethod_OptionCustom_ResolveMethod()
    {
        // Arrange & Act
        var endpoints = MapEndpoints<JsonTranscodingGreeterService>();

        // Assert
        var endpoint = FindGrpcEndpoint(endpoints, nameof(JsonTranscodingGreeterService.Custom));

        Assert.Equal("/v1/greeter/{name}", endpoint.RoutePattern.RawText);
        Assert.Equal("HEAD", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());
    }

    [Fact]
    public void AddMethod_OptionAdditionalBindings_ResolveMethods()
    {
        // Arrange & Act
        var endpoints = MapEndpoints<JsonTranscodingGreeterService>();

        var matchedEndpoints = FindGrpcEndpoints(endpoints, nameof(JsonTranscodingGreeterService.AdditionalBindings));

        // Assert
        Assert.Equal(2, matchedEndpoints.Count);

        var getMethodModel = matchedEndpoints[0];
        Assert.Equal("GET", getMethodModel.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());
        Assert.Equal("/v1/additional_bindings/{name}", getMethodModel.Metadata.GetMetadata<GrpcJsonTranscodingMetadata>()?.HttpRule.Get);
        Assert.Equal("/v1/additional_bindings/{name}", getMethodModel.RoutePattern.RawText);

        var additionalMethodModel = matchedEndpoints[1];
        Assert.Equal("DELETE", additionalMethodModel.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());
        Assert.Equal("/v1/additional_bindings/{name}", additionalMethodModel.Metadata.GetMetadata<GrpcJsonTranscodingMetadata>()?.HttpRule.Delete);
        Assert.Equal("/v1/additional_bindings/{name}", additionalMethodModel.RoutePattern.RawText);
    }

    [Fact]
    public void AddMethod_PatternVerb_RouteEndsWithVerb()
    {
        // Arrange & Act
        var endpoints = MapEndpoints<JsonTranscodingColonRouteService>();

        var startFrameImport = Assert.Single(FindGrpcEndpoints(endpoints, nameof(JsonTranscodingColonRouteService.StartFrameImport)));
        var getFrameImport = Assert.Single(FindGrpcEndpoints(endpoints, nameof(JsonTranscodingColonRouteService.GetFrameImport)));

        // Assert
        Assert.Equal("POST", startFrameImport.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());
        Assert.Equal("/v1/frames:startFrameImport", startFrameImport.Metadata.GetMetadata<GrpcJsonTranscodingMetadata>()?.HttpRule.Post);
        Assert.Equal("/v1/frames:startFrameImport", startFrameImport.RoutePattern.RawText);

        Assert.Equal("POST", getFrameImport.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());
        Assert.Equal("/v1/frames:getFrameImport", getFrameImport.Metadata.GetMetadata<GrpcJsonTranscodingMetadata>()?.HttpRule.Post);
        Assert.Equal("/v1/frames:getFrameImport", getFrameImport.RoutePattern.RawText);
    }

    [Fact]
    public void AddMethod_NoHttpRuleInProto_ThrowNotFoundError()
    {
        // Arrange & Act
        var endpoints = MapEndpoints<JsonTranscodingGreeterService>();

        // Assert
        var ex = Assert.Throws<InvalidOperationException>(() => FindGrpcEndpoint(endpoints, nameof(JsonTranscodingGreeterService.NoOption)));
        Assert.Equal("Couldn't find gRPC endpoint for method NoOption.", ex.Message);
    }

    [Fact]
    public void AddMethod_Success_HttpRuleFoundLogged()
    {
        // Arrange
        var testSink = new TestSink();
        var testProvider = new TestLoggerProvider(testSink);

        // Act
        var endpoints = MapEndpoints<JsonTranscodingGreeterService>(
            configureLogging: c =>
            {
                c.AddProvider(testProvider);
                c.SetMinimumLevel(LogLevel.Trace);
            });

        // Assert
        var write = testSink.Writes.Single(w =>
        {
            if (w.EventId.Name != "HttpRuleFound")
            {
                return false;
            }
            var values = (IReadOnlyList<KeyValuePair<string, object?>>)w.State;
            if ((string)values.Single(v => v.Key == "MethodName").Value! != "SayHello")
            {
                return false;
            }

            return true;
        });

        Assert.Equal(@"Found HttpRule mapping. Method SayHello on transcoding.JsonTranscodingGreeter. HttpRule payload: { ""get"": ""/v1/greeter/{name}"" }", write.Message);
    }

    [Fact]
    public void AddMethod_StreamingMethods_ThrowNotFoundError()
    {
        // Arrange
        var testSink = new TestSink();
        var testProvider = new TestLoggerProvider(testSink);

        // Act
        var endpoints = MapEndpoints<JsonTranscodingStreamingService>(
            configureLogging: c =>
            {
                c.AddProvider(testProvider);
                c.SetMinimumLevel(LogLevel.Trace);
            });

        // Assert
        Assert.Contains(testSink.Writes, c => c.Message == "Unable to bind GetClientStreaming on transcoding.JsonTranscodingStreaming to gRPC JSON transcoding. Client and bidirectional streaming methods are not supported.");
        Assert.Contains(testSink.Writes, c => c.Message == "Unable to bind GetBidiStreaming on transcoding.JsonTranscodingStreaming to gRPC JSON transcoding. Client and bidirectional streaming methods are not supported.");

        var matchedEndpoints = FindGrpcEndpoints(endpoints, nameof(JsonTranscodingStreamingService.GetServerStreaming));
        var endpoint = Assert.Single(matchedEndpoints);

        Assert.Equal("GET", endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods.Single());
        Assert.Equal("/v1/server_greeter/{name}", endpoint.Metadata.GetMetadata<GrpcJsonTranscodingMetadata>()?.HttpRule.Get);
        Assert.Equal("/v1/server_greeter/{name}", endpoint.RoutePattern.RawText);
    }

    [Fact]
    public void AddMethod_BadResponseBody_ThrowError()
    {
        // Arrange & Act
        var ex = Assert.Throws<InvalidOperationException>(() => MapEndpoints<JsonTranscodingInvalidResponseBodyGreeterService>());

        // Assert
        Assert.Equal("Error binding gRPC service 'JsonTranscodingInvalidResponseBodyGreeterService'.", ex.Message);
        Assert.Equal("Error binding BadResponseBody on JsonTranscodingInvalidResponseBodyGreeterService to HTTP API.", ex.InnerException!.InnerException!.Message);
        Assert.Equal("Couldn't find matching field for response body 'NoMatch' on HelloReply.", ex.InnerException!.InnerException!.InnerException!.Message);
    }

    [Fact]
    public void AddMethod_BadResponseBody_Nested_ThrowError()
    {
        // Arrange & Act
        var ex = Assert.Throws<InvalidOperationException>(() => MapEndpoints<JsonTranscodingInvalidNestedResponseBodyGreeterService>());

        // Assert
        Assert.Equal("Error binding gRPC service 'JsonTranscodingInvalidNestedResponseBodyGreeterService'.", ex.Message);
        Assert.Equal("Error binding BadResponseBody on JsonTranscodingInvalidNestedResponseBodyGreeterService to HTTP API.", ex.InnerException!.InnerException!.Message);
        Assert.Equal("The response body field 'sub.subfield' references a nested field. The response body field name must be on the top-level response message.", ex.InnerException!.InnerException!.InnerException!.Message);
    }

    [Fact]
    public void AddMethod_BadBody_ThrowError()
    {
        // Arrange & Act
        var ex = Assert.Throws<InvalidOperationException>(() => MapEndpoints<JsonTranscodingInvalidBodyGreeterService>());

        // Assert
        Assert.Equal("Error binding gRPC service 'JsonTranscodingInvalidBodyGreeterService'.", ex.Message);
        Assert.Equal("Error binding BadBody on JsonTranscodingInvalidBodyGreeterService to HTTP API.", ex.InnerException!.InnerException!.Message);
        Assert.Equal("Couldn't find matching field for body 'NoMatch' on HelloRequest.", ex.InnerException!.InnerException!.InnerException!.Message);
    }

    [Fact]
    public void AddMethod_BadBody_Nested_ThrowError()
    {
        // Arrange & Act
        var ex = Assert.Throws<InvalidOperationException>(() => MapEndpoints<JsonTranscodingInvalidNestedBodyGreeterService>());

        // Assert
        Assert.Equal("Error binding gRPC service 'JsonTranscodingInvalidNestedBodyGreeterService'.", ex.Message);
        Assert.Equal("Error binding BadBody on JsonTranscodingInvalidNestedBodyGreeterService to HTTP API.", ex.InnerException!.InnerException!.Message);
        Assert.Equal("The body field 'sub.subfield' references a nested field. The body field name must be on the top-level request message.", ex.InnerException!.InnerException!.InnerException!.Message);
    }

    [Fact]
    public void AddMethod_BadPattern_ThrowError()
    {
        // Arrange & Act
        var ex = Assert.Throws<InvalidOperationException>(() => MapEndpoints<JsonTranscodingInvalidPatternGreeterService>());

        // Assert
        Assert.Equal("Error binding gRPC service 'JsonTranscodingInvalidPatternGreeterService'.", ex.Message);
        Assert.Equal("Error binding BadPattern on JsonTranscodingInvalidPatternGreeterService to HTTP API.", ex.InnerException!.InnerException!.Message);
        Assert.Equal("Error parsing path template 'v1/greeter/{name}'.", ex.InnerException!.InnerException!.InnerException!.Message);
        Assert.Equal("Path template must start with a '/'.", ex.InnerException!.InnerException!.InnerException!.InnerException!.Message);
    }

    private static RouteEndpoint FindGrpcEndpoint(IReadOnlyList<Endpoint> endpoints, string methodName)
    {
        var e = FindGrpcEndpoints(endpoints, methodName).SingleOrDefault();
        if (e == null)
        {
            throw new InvalidOperationException($"Couldn't find gRPC endpoint for method {methodName}.");
        }

        return e;
    }

    private static List<RouteEndpoint> FindGrpcEndpoints(IReadOnlyList<Endpoint> endpoints, string methodName)
    {
        var e = endpoints
            .Where(e => e.Metadata.GetMetadata<GrpcMethodMetadata>()?.Method.Name == methodName)
            .Cast<RouteEndpoint>()
            .ToList();

        return e;
    }

    private class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public ICollection<EndpointDataSource> DataSources { get; }
        public IServiceProvider ServiceProvider { get; }

        public TestEndpointRouteBuilder(IServiceProvider serviceProvider)
        {
            DataSources = new List<EndpointDataSource>();
            ServiceProvider = serviceProvider;
        }

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return new ApplicationBuilder(ServiceProvider);
        }
    }

    private IReadOnlyList<Endpoint> MapEndpoints<TService>(Action<ILoggingBuilder>? configureLogging = null)
        where TService : class
    {
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(log =>
        {
            configureLogging?.Invoke(log);
        });
        var builder = serviceCollection.AddGrpc();
        serviceCollection.RemoveAll(typeof(IServiceMethodProvider<>));
        builder.AddJsonTranscoding();

        IEndpointRouteBuilder endpointRouteBuilder = new TestEndpointRouteBuilder(serviceCollection.BuildServiceProvider());

        endpointRouteBuilder.MapGrpcService<TService>();

        return endpointRouteBuilder.DataSources.Single().Endpoints;
    }
}
