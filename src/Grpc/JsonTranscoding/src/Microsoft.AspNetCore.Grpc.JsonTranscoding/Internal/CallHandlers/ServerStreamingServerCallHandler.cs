// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Grpc.Shared.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Grpc.JsonTranscoding.Internal.CallHandlers;

internal sealed class ServerStreamingServerCallHandler<TService, TRequest, TResponse> : ServerCallHandlerBase<TService, TRequest, TResponse>
    where TService : class
    where TRequest : class
    where TResponse : class
{
    private readonly ServerStreamingServerMethodInvoker<TService, TRequest, TResponse> _invoker;

    public ServerStreamingServerCallHandler(
        ServerStreamingServerMethodInvoker<TService, TRequest, TResponse> unaryMethodInvoker,
        ILoggerFactory loggerFactory,
        CallHandlerDescriptorInfo descriptorInfo,
        JsonSerializerOptions options) : base(unaryMethodInvoker, loggerFactory, descriptorInfo, options)
    {
        _invoker = unaryMethodInvoker;
    }

    protected override async Task HandleCallAsyncCore(HttpContext httpContext, JsonTranscodingServerCallContext serverCallContext)
    {
        // Decode request
        var request = await JsonRequestHelpers.ReadMessage<TRequest>(serverCallContext, SerializerOptions);

        var streamWriter = new HttpContextStreamWriter<TResponse>(serverCallContext, SerializerOptions);
        try
        {
            await _invoker.Invoke(httpContext, serverCallContext, request, streamWriter);
        }
        finally
        {
            streamWriter.Complete();
        }
    }

    protected override bool IsStreaming => true;
}
