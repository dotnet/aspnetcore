// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Grpc.Core;
using IntegrationTestsWebsite;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests;

public class UnaryTests : IntegrationTestBase
{
    public UnaryTests(GrpcTestFixture<Startup> fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    [Fact]
    public async Task GetWithRouteParameter_MatchUrl_SuccessResult()
    {
        // Arrange
        Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHello"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/v1/greeter/test").DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        using var result = await JsonDocument.ParseAsync(responseStream);

        // Assert
        Assert.Equal("Hello test!", result.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task WriteResponseHeadersAsync_SendHeaders_HeadersSentBeforeResult()
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            await context.WriteResponseHeadersAsync(new Metadata
            {
                new Metadata.Entry("test", "value!")
            });

            await tcs.Task;

            return new HelloReply { Message = $"Hello {request.Name}!" };
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHello"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/v1/greeter/test", HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        var resultTask = JsonDocument.ParseAsync(responseStream);

        // Assert
        Assert.Equal("value!", response.Headers.GetValues("test").Single());
        Assert.False(resultTask.IsCompleted);

        tcs.SetResult();
        using var result = await resultTask.DefaultTimeout();

        Assert.Equal("Hello test!", result.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task WriteResponseHeadersAsync_CallTwice_Error()
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            await context.WriteResponseHeadersAsync(new Metadata
            {
                new Metadata.Entry("test", "value!")
            });

            await tcs.Task;

            await context.WriteResponseHeadersAsync(new Metadata
            {
                new Metadata.Entry("test", "value 2!")
            });

            return new HelloReply { Message = $"Hello {request.Name}!" };
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHello"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/v1/greeter/test", HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        var resultTask = JsonDocument.ParseAsync(responseStream);

        // Assert
        Assert.Equal("value!", response.Headers.GetValues("test").Single());
        Assert.False(resultTask.IsCompleted);

        tcs.SetResult();
        using var result = await resultTask.DefaultTimeout();

        Assert.Equal("Exception was thrown by handler. InvalidOperationException: Response headers can only be sent once per call.", result.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task AuthContext_BasicRequest_Unauthenticated()
    {
        // Arrange
        AuthContext? authContext = null;
        Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            authContext = context.AuthContext;
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHello"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/v1/greeter/test").DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        using var result = await JsonDocument.ParseAsync(responseStream);

        // Assert
        Assert.False(authContext!.IsPeerAuthenticated);
        Assert.Equal("Hello test!", result.RootElement.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("utf-8")]
    [InlineData("utf-16")]
    [InlineData("latin1")]
    public async Task Request_SupportedCharset_Success(string? charset)
    {
        // Arrange
        Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHelloPost"));

        var encoding = JsonRequestHelpers.GetEncodingFromCharset(charset);
        var contentType = charset != null
            ? "application/json; charset=" + charset
            : "application/json";

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        var requestMessage = new HelloRequest { Name = "test" };
        var content = new ByteArrayContent((encoding ?? Encoding.UTF8).GetBytes(requestMessage.ToString()));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        // Act
        var response = await client.PostAsync("/v1/greeter", content).DefaultTimeout();
        var responseText = await response.Content.ReadAsStringAsync();
        using var result = JsonDocument.Parse(responseText);

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal(encoding?.WebName ?? "utf-8", response.Content.Headers.ContentType!.CharSet);
        Assert.Equal("Hello test!", result.RootElement.GetProperty("message").GetString());
    }

    [Theory]
    [InlineData("FAKE", "InvalidOperationException: Unable to read the request as JSON because the request content type charset 'FAKE' is not a known encoding.")]
    [InlineData("UTF-7", "InvalidOperationException: Unable to read the request as JSON because the request content type charset 'UTF-7' is not a known encoding.")]
    public async Task Request_UnsupportedCharset_Error(string? charset, string errorMessage)
    {
        // Arrange
        Task<HelloReply> UnaryMethod(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<HelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHelloPost"));

        var contentType = "application/json; charset=" + charset;

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        var requestMessage = new HelloRequest { Name = "test" };
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(requestMessage.ToString()));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        // Act
        var response = await client.PostAsync("/v1/greeter", content).DefaultTimeout();
        var responseText = await response.Content.ReadAsStringAsync();
        using var result = JsonDocument.Parse(responseText);

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType!.CharSet);
        Assert.Contains(errorMessage, result.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Request_SendEnumString_Success()
    {
        // Arrange
        Task<HelloReply> UnaryMethod(EnumHelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddUnaryMethod<EnumHelloRequest, HelloReply>(
            UnaryMethod,
            Greeter.Descriptor.FindMethodByName("SayHelloPostEnum"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        var requestMessage = new EnumHelloRequest { Name = NameOptions.Jane };
        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(requestMessage.ToString()));
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json");

        // Act
        var response = await client.PostAsync("/v1/greeter_enum", content).DefaultTimeout();
        var responseText = await response.Content.ReadAsStringAsync();
        using var result = JsonDocument.Parse(responseText);

        // Assert
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("utf-8", response.Content.Headers.ContentType!.CharSet);
        Assert.Equal("Hello Jane!", result.RootElement.GetProperty("message").GetString());
    }
}
