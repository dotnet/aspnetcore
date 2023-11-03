// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Grpc.Core;
using IntegrationTestsWebsite;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests;

public class RouteTests : IntegrationTestBase
{
    public RouteTests(GrpcTestFixture<Startup> fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    [Fact]
    public async Task ComplexParameter_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHelloComplex"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/v1/greeter/from/test").DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        using var result = await JsonDocument.ParseAsync(responseStream);

        // Assert
        Assert.Equal("Hello from/test!", result.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task MultipleComplexCatchAll_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod1(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"One - Hello {request.Name}!" });
        }
        Task<HelloReply> UnaryMethod2(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Two - Hello {request.Name}!" });
        }
        var method1 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod1,
            Greeter.Descriptor.FindMethodByName("SayHelloComplexCatchAll1"));
        var method2 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod2,
            Greeter.Descriptor.FindMethodByName("SayHelloComplexCatchAll2"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act 1
        var response1 = await client.GetAsync("/v1/greeter/test1/b/c/d/one").DefaultTimeout();
        var responseStream1 = await response1.Content.ReadAsStreamAsync();
        using var result1 = await JsonDocument.ParseAsync(responseStream1);

        // Assert 1
        Assert.Equal("One - Hello v1/greeter/test1/b/c!", result1.RootElement.GetProperty("message").GetString());

        // Act 2
        var response2 = await client.GetAsync("/v1/greeter/test2/b/c/d/two").DefaultTimeout();
        var responseStream2 = await response2.Content.ReadAsStreamAsync();
        using var result2 = await JsonDocument.ParseAsync(responseStream2);

        // Assert 2
        Assert.Equal("Two - Hello v1/greeter/test2/b/c!", result2.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ComplexCatchAllParameter_NestedField_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod(ComplextHelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name.FirstName} {request.Name.LastName}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<ComplextHelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHelloComplexCatchAll3"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/v1/last_name/complex_greeter/test2/b/c/d/two").DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        using var result = await JsonDocument.ParseAsync(responseStream);

        // Assert
        Assert.Equal("Hello complex_greeter/test2/b last_name!", result.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SimpleCatchAllParameter_PrefixSuffixSlashes_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHelloComplexCatchAll4"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/v1/greeter//name/one/two//").DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        using var result = await JsonDocument.ParseAsync(responseStream);

        // Assert
        Assert.Equal("Hello /name/one/two//!", result.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task ParameterVerb_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod1(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} one!" });
        }
        Task<HelloReply> UnaryMethod2(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} two!" });
        }
        var method1 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod1,
            Greeter.Descriptor.FindMethodByName("SayHelloCustomVerbOne"));
        var method2 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod2,
            Greeter.Descriptor.FindMethodByName("SayHelloCustomVerbTwo"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act 1
        var response1 = await client.GetAsync("/v1/greeter_custom/test:one").DefaultTimeout();
        var responseStream1 = await response1.Content.ReadAsStreamAsync();
        using var result1 = await JsonDocument.ParseAsync(responseStream1);

        // Assert 2
        Assert.Equal("Hello test one!", result1.RootElement.GetProperty("message").GetString());

        // Act 2
        var response2 = await client.GetAsync("/v1/greeter_custom/test:two").DefaultTimeout();
        var responseStream2 = await response2.Content.ReadAsStreamAsync();
        using var result2 = await JsonDocument.ParseAsync(responseStream2);

        // Assert 2
        Assert.Equal("Hello test two!", result2.RootElement.GetProperty("message").GetString());

        // Act 3
        var response3 = await client.GetAsync("/v1/greeter_custom/test").DefaultTimeout();

        // Assert 3
        Assert.Equal(HttpStatusCode.NotFound, response3.StatusCode);
    }

    [Fact]
    public async Task CatchAllVerb_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod1(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} one!" });
        }
        Task<HelloReply> UnaryMethod2(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} two!" });
        }
        var method1 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod1,
            Greeter.Descriptor.FindMethodByName("SayHelloCatchAllCustomVerbOne"));
        var method2 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod2,
            Greeter.Descriptor.FindMethodByName("SayHelloCatchAllCustomVerbTwo"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act 1
        var response1 = await client.GetAsync("/v1/greeter_customcatchall/test/name:one").DefaultTimeout();
        var responseStream1 = await response1.Content.ReadAsStreamAsync();
        using var result1 = await JsonDocument.ParseAsync(responseStream1);

        // Assert 2
        Assert.Equal("Hello test/name one!", result1.RootElement.GetProperty("message").GetString());

        // Act 2
        var response2 = await client.GetAsync("/v1/greeter_customcatchall/test/name:two").DefaultTimeout();
        var responseStream2 = await response2.Content.ReadAsStreamAsync();
        using var result2 = await JsonDocument.ParseAsync(responseStream2);

        // Assert 2
        Assert.Equal("Hello test/name two!", result2.RootElement.GetProperty("message").GetString());

        // Act 3
        var response3 = await client.GetAsync("/v1/greeter_customcatchall/test/name").DefaultTimeout();

        // Assert 3
        Assert.Equal(HttpStatusCode.NotFound, response3.StatusCode);
    }

    [Fact]
    public async Task PostVerb_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod1(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} one!" });
        }
        Task<HelloReply> UnaryMethod2(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} two!" });
        }
        var method1 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod1,
            Greeter.Descriptor.FindMethodByName("SayHelloPostCustomVerbOne"));
        var method2 = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod2,
            Greeter.Descriptor.FindMethodByName("SayHelloPostCustomVerbTwo"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        var requestMessage = new HelloRequest { Name = "test" };
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(requestMessage.ToString()));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        // Act 1
        var response1 = await client.PostAsync("/v1/greeter_custompost:one", content).DefaultTimeout();
        var responseStream1 = await response1.Content.ReadAsStreamAsync();
        using var result1 = await JsonDocument.ParseAsync(responseStream1);

        // Assert 2
        Assert.Equal("Hello test one!", result1.RootElement.GetProperty("message").GetString());

        // Act 2
        var response2 = await client.PostAsync("/v1/greeter_custompost:two", content).DefaultTimeout();
        var responseStream2 = await response2.Content.ReadAsStreamAsync();
        using var result2 = await JsonDocument.ParseAsync(responseStream2);

        // Assert 2
        Assert.Equal("Hello test two!", result2.RootElement.GetProperty("message").GetString());

        // Act 3
        var response3 = await client.PostAsync("/v1/greeter_custompost", content).DefaultTimeout();

        // Assert 3
        Assert.Equal(HttpStatusCode.NotFound, response3.StatusCode);
    }
}
