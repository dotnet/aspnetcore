// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Grpc.Core;
using Grpc.Shared.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Grpc.HttpApi.Internal.CallHandlers;

internal sealed class UnaryServerCallHandler<TService, TRequest, TResponse> : ServerCallHandlerBase<TService, TRequest, TResponse>
    where TService : class
    where TRequest : class
    where TResponse : class
{
    private readonly UnaryServerMethodInvoker<TService, TRequest, TResponse> _invoker;

    public UnaryServerCallHandler(
        UnaryServerMethodInvoker<TService, TRequest, TResponse> unaryMethodInvoker,
        ILoggerFactory loggerFactory,
        CallHandlerDescriptorInfo descriptorInfo,
        JsonSerializerOptions options) : base(unaryMethodInvoker, loggerFactory, descriptorInfo, options)
    {
        _invoker = unaryMethodInvoker;
    }

    protected override async Task HandleCallAsyncCore(HttpContext httpContext, HttpApiServerCallContext serverCallContext)
    {
        var request = await JsonRequestHelpers.ReadMessage<TRequest>(serverCallContext, SerializerOptions);

        var response = await _invoker.Invoke(httpContext, serverCallContext, request);

        if (serverCallContext.Status.StatusCode != StatusCode.OK)
        {
            throw new RpcException(serverCallContext.Status);
        }

        if (response == null)
        {
            // This is consistent with Grpc.Core when a null value is returned
            throw new RpcException(new Status(StatusCode.Cancelled, "No message returned from method."));
        }

        serverCallContext.EnsureResponseHeaders();

        await JsonRequestHelpers.SendMessage(serverCallContext, SerializerOptions, response);
    }
}
