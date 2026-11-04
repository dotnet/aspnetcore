// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Text.Json;
using Grpc.Core;
using IntegrationTestsWebsite;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.IntegrationTests;

public class ServerStreamingTests : IntegrationTestBase
{
    public ServerStreamingTests(GrpcTestFixture<Startup> fixture, ITestOutputHelper outputHelper)
        : base(fixture, outputHelper)
    {
    }

    [Fact]
    public async Task GetWithRouteParameter_WriteOne_SuccessResult()
    {
        // Arrange
        async Task ServerStreamingMethod(HelloRequest request, IServerStreamWriter<HelloReply> writer, ServerCallContext context)
        {
            await writer.WriteAsync(new HelloReply { Message = $"Hello {request.Name}!" });
        }
        var method = Fixture.DynamicGrpc.AddServerStreamingMethod<HelloRequest, HelloReply>(
            ServerStreamingMethod,
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
    public async Task GetWithRouteParameter_WriteMultiple_SuccessResult()
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task ServerStreamingMethod(HelloRequest request, IServerStreamWriter<HelloReply> writer, ServerCallContext context)
        {
            await writer.WriteAsync(new HelloReply { Message = $"Hello {request.Name} 1!" });
            await tcs.Task;
            await writer.WriteAsync(new HelloReply { Message = $"Hello {request.Name} 2!" });
        }
        var method = Fixture.DynamicGrpc.AddServerStreamingMethod<HelloRequest, HelloReply>(
            ServerStreamingMethod,
            Greeter.Descriptor.FindMethodByName("SayHello"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act 1
        var response = await client.GetAsync("/v1/greeter/test", HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        var streamReader = new StreamReader(responseStream);

        var line1 = await streamReader.ReadLineAsync();
        using var result1 = JsonDocument.Parse(line1!);

        // Assert 1
        Assert.Equal("Hello test 1!", result1.RootElement.GetProperty("message").GetString());

        // Act 2
        tcs.SetResult();
        var line2 = await streamReader.ReadLineAsync();
        using var result2 = JsonDocument.Parse(line2!);

        // Assert 2
        Assert.Equal("Hello test 2!", result2.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task GetWithRouteParameter_WriteMultiple_CancellationBefore_CallCanceled()
    {
        // Arrange
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task ServerStreamingMethod(HelloRequest request, IServerStreamWriter<HelloReply> writer, ServerCallContext context)
        {
            await writer.WriteAsync(new HelloReply { Message = $"Hello {request.Name} 1!" });
            await tcs.Task;
            await writer.WriteAsync(new HelloReply { Message = $"Hello {request.Name} 2!" }, new CancellationToken(canceled: true));
        }
        var method = Fixture.DynamicGrpc.AddServerStreamingMethod<HelloRequest, HelloReply>(
            ServerStreamingMethod,
            Greeter.Descriptor.FindMethodByName("SayHello"));

        var client = new HttpClient(Fixture.Handler) { BaseAddress = new Uri("http://localhost") };

        // Act 1
        var response = await client.GetAsync("/v1/greeter/test", HttpCompletionOption.ResponseHeadersRead).DefaultTimeout();
        var responseStream = await response.Content.ReadAsStreamAsync();
        var streamReader = new StreamReader(responseStream);

        var line1 = await streamReader.ReadLineAsync();
        using var result1 = JsonDocument.Parse(line1!);

        // Assert 1
        Assert.Equal("Hello test 1!", result1.RootElement.GetProperty("message").GetString());

        // Act & Assert 2
        tcs.SetResult();
        await Assert.ThrowsAsync<IOException>(() => streamReader.ReadLineAsync());
    }
}
