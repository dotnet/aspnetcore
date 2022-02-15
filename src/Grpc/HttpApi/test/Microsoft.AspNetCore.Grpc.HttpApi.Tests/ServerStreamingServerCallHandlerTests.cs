// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Grpc.Shared.Server;
using Grpc.Tests.Shared;
using HttpApi;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal.CallHandlers;
using Microsoft.AspNetCore.Grpc.HttpApi.Internal.Json;
using Microsoft.AspNetCore.Grpc.HttpApi.Tests.Infrastructure;
using Microsoft.AspNetCore.Testing;
using Xunit.Abstractions;
using MethodOptions = Grpc.Shared.Server.MethodOptions;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Tests;

public class ServerStreamingServerCallHandlerTests : LoggedTest
{
    public ServerStreamingServerCallHandlerTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task HandleCallAsync_WriteMultipleMessages_Returned()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        ServerStreamingServerMethod<HttpApiGreeterService, HelloRequest, HelloReply> invoker = async (s, r, w, c) =>
        {
            await w.WriteAsync(new HelloReply { Message = $"Hello {r.Name} 1" });
            await syncPoint.WaitToContinue();
            await w.WriteAsync(new HelloReply { Message = $"Hello {r.Name} 2" });
        };

        var pipe = new Pipe();

        var routeParameterDescriptors = new Dictionary<string, List<FieldDescriptor>>
        {
            ["name"] = new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) })
        };
        var descriptorInfo = TestHelpers.CreateDescriptorInfo(routeParameterDescriptors: routeParameterDescriptors);
        var callHandler = CreateCallHandler(invoker, descriptorInfo: descriptorInfo);
        var httpContext = TestHelpers.CreateHttpContext(bodyStream: pipe.Writer.AsStream());
        httpContext.Request.RouteValues["name"] = "TestName!";

        // Act
        var callTask = callHandler.HandleCallAsync(httpContext);

        // Assert
        var line1 = await ReadLineAsync(pipe.Reader).DefaultTimeout();
        using var responseJson1 = JsonDocument.Parse(line1!);
        Assert.Equal("Hello TestName! 1", responseJson1.RootElement.GetProperty("message").GetString());

        await syncPoint.WaitForSyncPoint().DefaultTimeout();
        syncPoint.Continue();

        var line2 = await ReadLineAsync(pipe.Reader).DefaultTimeout();
        using var responseJson2 = JsonDocument.Parse(line2!);
        Assert.Equal("Hello TestName! 2", responseJson2.RootElement.GetProperty("message").GetString());

        await callTask.DefaultTimeout();
    }

    [Fact]
    public async Task HandleCallAsync_MessageThenError_MessageThenErrorReturned()
    {
        // Arrange
        ServerStreamingServerMethod<HttpApiGreeterService, HelloRequest, HelloReply> invoker = async (s, r, w, c) =>
        {
            await w.WriteAsync(new HelloReply { Message = $"Hello {r.Name} 1" });
            throw new Exception("Exception!");
        };

        var pipe = new Pipe();

        var routeParameterDescriptors = new Dictionary<string, List<FieldDescriptor>>
        {
            ["name"] = new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) })
        };
        var descriptorInfo = TestHelpers.CreateDescriptorInfo(routeParameterDescriptors: routeParameterDescriptors);
        var callHandler = CreateCallHandler(invoker, descriptorInfo: descriptorInfo);
        var httpContext = TestHelpers.CreateHttpContext(bodyStream: pipe.Writer.AsStream());
        httpContext.Request.RouteValues["name"] = "TestName!";

        // Act
        var callTask = callHandler.HandleCallAsync(httpContext);

        // Assert
        var line1 = await ReadLineAsync(pipe.Reader).DefaultTimeout();
        using var responseJson1 = JsonDocument.Parse(line1!);
        Assert.Equal("Hello TestName! 1", responseJson1.RootElement.GetProperty("message").GetString());

        var line2 = await ReadLineAsync(pipe.Reader).DefaultTimeout();
        using var responseJson2 = JsonDocument.Parse(line2!);
        Assert.Equal("Exception was thrown by handler.", responseJson2.RootElement.GetProperty("message").GetString());
        Assert.Equal("Exception was thrown by handler.", responseJson2.RootElement.GetProperty("error").GetString());
        Assert.Equal(2, responseJson2.RootElement.GetProperty("code").GetInt32());

        await callTask.DefaultTimeout();
    }

    [Fact]
    public async Task HandleCallAsync_ErrorWithDetailedErrors_DetailedErrorResponse()
    {
        // Arrange
        ServerStreamingServerMethod<HttpApiGreeterService, HelloRequest, HelloReply> invoker = (s, r, w, c) =>
        {
            return Task.FromException<HelloReply>(new Exception("Exception!"));
        };

        var pipe = new Pipe();

        var routeParameterDescriptors = new Dictionary<string, List<FieldDescriptor>>
        {
            ["name"] = new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) })
        };
        var descriptorInfo = TestHelpers.CreateDescriptorInfo(routeParameterDescriptors: routeParameterDescriptors);
        var serviceOptions = new GrpcServiceOptions { EnableDetailedErrors = true };
        var callHandler = CreateCallHandler(invoker, descriptorInfo: descriptorInfo, serviceOptions: serviceOptions);
        var httpContext = TestHelpers.CreateHttpContext(bodyStream: pipe.Writer.AsStream());
        httpContext.Request.RouteValues["name"] = "TestName!";

        // Act
        var callTask = callHandler.HandleCallAsync(httpContext);

        // Assert
        var line = await ReadLineAsync(pipe.Reader).DefaultTimeout();
        using var responseJson = JsonDocument.Parse(line!);
        Assert.Equal("Exception was thrown by handler. Exception: Exception!", responseJson.RootElement.GetProperty("message").GetString());
        Assert.Equal("Exception was thrown by handler. Exception: Exception!", responseJson.RootElement.GetProperty("error").GetString());
        Assert.Equal(2, responseJson.RootElement.GetProperty("code").GetInt32());

        await callTask.DefaultTimeout();
    }

    public async Task<string?> ReadLineAsync(PipeReader pipeReader)
    {
        string? str;

        while (true)
        {
            var result = await pipeReader.ReadAsync();
            var buffer = result.Buffer;

            if ((str = ReadLine(ref buffer, out var end)) is not null)
            {
                pipeReader.AdvanceTo(end, end);
                return str;
            }

            pipeReader.AdvanceTo(buffer.Start, buffer.End);

            if (result.IsCompleted)
            {
                break;
            }
        }

        return str;
    }

    private static string? ReadLine(ref ReadOnlySequence<byte> buffer, out SequencePosition end)
    {
        var reader = new SequenceReader<byte>(buffer);

        if (reader.TryReadTo(out ReadOnlySequence<byte> line, (byte)'\n'))
        {
            buffer = buffer.Slice(reader.Position);
            end = reader.Position;

            return Encoding.UTF8.GetString(line);
        }

        end = default;
        return null;
    }

    private ServerStreamingServerCallHandler<HttpApiGreeterService, HelloRequest, HelloReply> CreateCallHandler(
        ServerStreamingServerMethod<HttpApiGreeterService, HelloRequest, HelloReply> invoker,
        CallHandlerDescriptorInfo? descriptorInfo = null,
        List<(Type Type, object[] Args)>? interceptors = null,
        GrpcHttpApiOptions? httpApiOptions = null,
        GrpcServiceOptions? serviceOptions = null)
    {
        serviceOptions ??= new GrpcServiceOptions();
        if (interceptors != null)
        {
            foreach (var interceptor in interceptors)
            {
                serviceOptions.Interceptors.Add(interceptor.Type, interceptor.Args ?? Array.Empty<object>());
            }
        }

        var callInvoker = new ServerStreamingServerMethodInvoker<HttpApiGreeterService, HelloRequest, HelloReply>(
            invoker,
            CreateServiceMethod<HelloRequest, HelloReply>("TestMethodName", HelloRequest.Parser, HelloReply.Parser),
            MethodOptions.Create(new[] { serviceOptions }),
            new TestGrpcServiceActivator<HttpApiGreeterService>());

        var jsonSettings = httpApiOptions?.JsonSettings ?? new JsonSettings() { WriteIndented = false };

        return new ServerStreamingServerCallHandler<HttpApiGreeterService, HelloRequest, HelloReply>(
            callInvoker,
            LoggerFactory,
            descriptorInfo ?? TestHelpers.CreateDescriptorInfo(),
            JsonConverterHelper.CreateSerializerOptions(jsonSettings));
    }

    public static Marshaller<TMessage> GetMarshaller<TMessage>(MessageParser<TMessage> parser) where TMessage : IMessage<TMessage> =>
        Marshallers.Create<TMessage>(r => r.ToByteArray(), data => parser.ParseFrom(data));

    public static readonly Method<HelloRequest, HelloReply> ServiceMethod = CreateServiceMethod("MethodName", HelloRequest.Parser, HelloReply.Parser);

    public static Method<TRequest, TResponse> CreateServiceMethod<TRequest, TResponse>(string methodName, MessageParser<TRequest> requestParser, MessageParser<TResponse> responseParser)
         where TRequest : IMessage<TRequest>
         where TResponse : IMessage<TResponse>
    {
        return new Method<TRequest, TResponse>(MethodType.Unary, "ServiceName", methodName, GetMarshaller(requestParser), GetMarshaller(responseParser));
    }
}
