// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Google.Api;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Grpc.Shared;
using Grpc.Shared.Server;
using Grpc.Tests.Shared;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.Json;
using Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Transcoding;
using Xunit.Abstractions;
using MethodOptions = Grpc.Shared.Server.MethodOptions;
using Type = System.Type;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Tests;

public class ServerStreamingServerCallHandlerTests : LoggedTest
{
    public ServerStreamingServerCallHandlerTests(ITestOutputHelper output) : base(output) { }

    [Fact]
    public async Task HandleCallAsync_WriteMultipleMessages_Returned()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        ServerStreamingServerMethod<JsonTranscodingGreeterService, HelloRequest, HelloReply> invoker = async (s, r, w, c) =>
        {
            await w.WriteAsync(new HelloReply { Message = $"Hello {r.Name} 1" });
            await syncPoint.WaitToContinue();
            await w.WriteAsync(new HelloReply { Message = $"Hello {r.Name} 2" });
        };

        var pipe = new Pipe();

        var descriptorPath = new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) });
        var routeParameterDescriptors = new Dictionary<string, RouteParameter>
        {
            ["name"] = CreateRouteParameter(descriptorPath)
        };
        var descriptorInfo = TestHelpers.CreateDescriptorInfo(routeParameterDescriptors: routeParameterDescriptors);
        var callHandler = CreateCallHandler(invoker, descriptorInfo: descriptorInfo);
        var httpContext = TestHelpers.CreateHttpContext(bodyStream: pipe.Writer.AsStream());
        httpContext.Request.RouteValues["name"] = "TestName!";

        // Act
        var callTask = callHandler.HandleCallAsync(httpContext);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);

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

    private static RouteParameter CreateRouteParameter(List<FieldDescriptor> descriptorPath)
    {
        return new RouteParameter(descriptorPath, new HttpRouteVariable(), string.Empty);
    }

    [Fact]
    public async Task HandleCallAsync_MessageThenError_MessageThenErrorReturned()
    {
        // Arrange
        ServerStreamingServerMethod<JsonTranscodingGreeterService, HelloRequest, HelloReply> invoker = async (s, r, w, c) =>
        {
            await w.WriteAsync(new HelloReply { Message = $"Hello {r.Name} 1" });
            throw new Exception("Exception!");
        };

        var pipe = new Pipe();

        var routeParameterDescriptors = new Dictionary<string, RouteParameter>
        {
            ["name"] = CreateRouteParameter(new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) }))
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
        Assert.Equal(2, responseJson2.RootElement.GetProperty("code").GetInt32());

        var exceptionWrite = TestSink.Writes.Single(w => w.EventId.Name == "ErrorExecutingServiceMethod");
        Assert.Equal("Error when executing service method 'TestMethodName'.", exceptionWrite.Message);
        Assert.Equal("Exception!", exceptionWrite.Exception.Message);

        await callTask.DefaultTimeout();
    }

    [Fact]
    public async Task HandleCallAsync_MessageThenRpcException_MessageThenErrorReturned()
    {
        // Arrange
        var debugException = new Exception("Error!");
        ServerStreamingServerMethod<JsonTranscodingGreeterService, HelloRequest, HelloReply> invoker = async (s, r, w, c) =>
        {
            await w.WriteAsync(new HelloReply { Message = $"Hello {r.Name} 1" });
            throw new RpcException(new Status(StatusCode.Aborted, "Detail!", debugException));
        };

        var pipe = new Pipe();

        var routeParameterDescriptors = new Dictionary<string, RouteParameter>
        {
            ["name"] = CreateRouteParameter(new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) }))
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
        Assert.Equal("Detail!", responseJson2.RootElement.GetProperty("message").GetString());
        Assert.Equal((int)StatusCode.Aborted, responseJson2.RootElement.GetProperty("code").GetInt32());

        var exceptionWrite = TestSink.Writes.Single(w => w.EventId.Name == "RpcConnectionError");
        Assert.Equal("Error status code 'Aborted' with detail 'Detail!' raised.", exceptionWrite.Message);
        Assert.Equal(debugException, exceptionWrite.Exception);

        await callTask.DefaultTimeout();
    }

    [Fact]
    public async Task HandleCallAsync_ErrorWithDetailedErrors_DetailedErrorResponse()
    {
        // Arrange
        ServerStreamingServerMethod<JsonTranscodingGreeterService, HelloRequest, HelloReply> invoker = (s, r, w, c) =>
        {
            return Task.FromException<HelloReply>(new Exception("Exception!"));
        };

        var pipe = new Pipe();

        var routeParameterDescriptors = new Dictionary<string, RouteParameter>
        {
            ["name"] = CreateRouteParameter(new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) }))
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
        Assert.Equal(2, responseJson.RootElement.GetProperty("code").GetInt32());

        var exceptionWrite = TestSink.Writes.Single(w => w.EventId.Name == "ErrorExecutingServiceMethod");
        Assert.Equal("Error when executing service method 'TestMethodName'.", exceptionWrite.Message);
        Assert.Equal("Exception!", exceptionWrite.Exception.Message);

        await callTask.DefaultTimeout();
    }

    [Fact]
    public async Task HandleCallAsync_HttpBody_WriteMultipleMessages_Returned()
    {
        // Arrange
        var syncPoint = new SyncPoint();

        ServerStreamingServerMethod<JsonTranscodingGreeterService, HelloRequest, HttpBody> invoker = async (s, r, w, c) =>
        {
            await w.WriteAsync(new HttpBody
            {
                ContentType = "application/xml",
                Data = ByteString.CopyFrom(Encoding.UTF8.GetBytes($"<message>Hello {r.Name} 1</message>"))
            });
            await syncPoint.WaitToContinue();
            await w.WriteAsync(new HttpBody
            {
                ContentType = "application/xml",
                Data = ByteString.CopyFrom(Encoding.UTF8.GetBytes($"<message>Hello {r.Name} 2</message>"))
            });
        };

        var pipe = new Pipe();

        var routeParameterDescriptors = new Dictionary<string, RouteParameter>
        {
            ["name"] = CreateRouteParameter(new List<FieldDescriptor>(new[] { HelloRequest.Descriptor.FindFieldByNumber(HelloRequest.NameFieldNumber) }))
        };
        var descriptorInfo = TestHelpers.CreateDescriptorInfo(routeParameterDescriptors: routeParameterDescriptors);
        var callHandler = CreateCallHandler(
            invoker,
            CreateServiceMethod("HttpResponseBody", HelloRequest.Parser, HttpBody.Parser),
            descriptorInfo: descriptorInfo);
        var httpContext = TestHelpers.CreateHttpContext(bodyStream: pipe.Writer.AsStream());
        httpContext.Request.RouteValues["name"] = "TestName!";

        // Act
        var callTask = callHandler.HandleCallAsync(httpContext);

        // Assert
        Assert.Equal(200, httpContext.Response.StatusCode);
        Assert.Equal("application/xml", httpContext.Response.ContentType);

        var line1 = await ReadLineAsync(pipe.Reader).DefaultTimeout();
        var responseXml1 = XDocument.Parse(line1!);
        Assert.Equal("Hello TestName! 1", (string)responseXml1.Element("message")!);

        await syncPoint.WaitForSyncPoint().DefaultTimeout();
        syncPoint.Continue();

        var line2 = await ReadLineAsync(pipe.Reader).DefaultTimeout();
        var responseXml2 = XDocument.Parse(line2!);
        Assert.Equal("Hello TestName! 2", (string)responseXml2.Element("message")!);

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

    private ServerStreamingServerCallHandler<JsonTranscodingGreeterService, HelloRequest, HelloReply> CreateCallHandler(
        ServerStreamingServerMethod<JsonTranscodingGreeterService, HelloRequest, HelloReply> invoker,
        CallHandlerDescriptorInfo? descriptorInfo = null,
        List<(Type Type, object[] Args)>? interceptors = null,
        GrpcJsonTranscodingOptions? jsonTranscodingOptions = null,
        GrpcServiceOptions? serviceOptions = null)
    {
        return CreateCallHandler(
            invoker,
            CreateServiceMethod("TestMethodName", HelloRequest.Parser, HelloReply.Parser),
            descriptorInfo,
            interceptors,
            jsonTranscodingOptions,
            serviceOptions);
    }

    private ServerStreamingServerCallHandler<JsonTranscodingGreeterService, TRequest, TResponse> CreateCallHandler<TRequest, TResponse>(
        ServerStreamingServerMethod<JsonTranscodingGreeterService, TRequest, TResponse> invoker,
        Method<TRequest, TResponse> method,
        CallHandlerDescriptorInfo? descriptorInfo = null,
        List<(Type Type, object[] Args)>? interceptors = null,
        GrpcJsonTranscodingOptions? jsonTranscodingOptions = null,
        GrpcServiceOptions? serviceOptions = null)
        where TRequest : class, IMessage<TRequest>
        where TResponse : class, IMessage<TResponse>
    {
        serviceOptions ??= new GrpcServiceOptions();
        if (interceptors != null)
        {
            foreach (var interceptor in interceptors)
            {
                serviceOptions.Interceptors.Add(interceptor.Type, interceptor.Args ?? Array.Empty<object>());
            }
        }

        var callInvoker = new ServerStreamingServerMethodInvoker<JsonTranscodingGreeterService, TRequest, TResponse>(
            invoker,
            method,
            MethodOptions.Create(new[] { serviceOptions }),
            new TestGrpcServiceActivator<JsonTranscodingGreeterService>());

        var jsonSettings = jsonTranscodingOptions?.JsonSettings ?? new GrpcJsonSettings() { WriteIndented = false };

        var descriptorRegistry = new DescriptorRegistry();
        descriptorRegistry.RegisterFileDescriptor(TestHelpers.GetMessageDescriptor(typeof(TRequest)).File);
        descriptorRegistry.RegisterFileDescriptor(TestHelpers.GetMessageDescriptor(typeof(TResponse)).File);

        var jsonContext = new JsonContext(jsonSettings, jsonTranscodingOptions?.TypeRegistry ?? TypeRegistry.Empty, descriptorRegistry);

        return new ServerStreamingServerCallHandler<JsonTranscodingGreeterService, TRequest, TResponse>(
            callInvoker,
            LoggerFactory,
            descriptorInfo ?? TestHelpers.CreateDescriptorInfo(),
            JsonConverterHelper.CreateSerializerOptions(jsonContext));
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
