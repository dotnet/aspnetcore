#region Copyright notice and license

// Copyright 2019 The gRPC Authors
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using System.Diagnostics.CodeAnalysis;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Grpc.Shared.Server;

/// <summary>
/// Duplex streaming server method invoker.
/// </summary>
/// <typeparam name="TService">Service type for this method.</typeparam>
/// <typeparam name="TRequest">Request message type for this method.</typeparam>
/// <typeparam name="TResponse">Response message type for this method.</typeparam>
internal sealed class DuplexStreamingServerMethodInvoker<[DynamicallyAccessedMembers(ServerDynamicAccessConstants.ServiceAccessibility)] TService, TRequest, TResponse> : ServerMethodInvokerBase<TService, TRequest, TResponse>
    where TRequest : class
    where TResponse : class
    where TService : class
{
    private readonly DuplexStreamingServerMethod<TService, TRequest, TResponse> _invoker;
    private readonly DuplexStreamingServerMethod<TRequest, TResponse>? _pipelineInvoker;

    /// <summary>
    /// Creates a new instance of <see cref="DuplexStreamingServerMethodInvoker{TService, TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="invoker">The duplex streaming method to invoke.</param>
    /// <param name="method">The description of the gRPC method.</param>
    /// <param name="options">The options used to execute the method.</param>
    /// <param name="serviceActivator">The service activator used to create service instances.</param>
    /// <param name="interceptorActivators">The interceptor activators used to create interceptor instances.</param>
    public DuplexStreamingServerMethodInvoker(
        DuplexStreamingServerMethod<TService, TRequest, TResponse> invoker,
        Method<TRequest, TResponse> method,
        MethodOptions options,
        IGrpcServiceActivator<TService> serviceActivator,
        InterceptorActivators interceptorActivators)
        : base(method, options, serviceActivator)
    {
        _invoker = invoker;

        if (Options.HasInterceptors)
        {
            var interceptorPipeline = new InterceptorPipelineBuilder<TRequest, TResponse>(Options.Interceptors, interceptorActivators);
            _pipelineInvoker = interceptorPipeline.DuplexStreamingPipeline(ResolvedInterceptorInvoker);
        }
    }

    private async Task ResolvedInterceptorInvoker(IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream, ServerCallContext resolvedContext)
    {
        GrpcActivatorHandle<TService> serviceHandle = default;
        try
        {
            serviceHandle = CreateServiceHandle(resolvedContext);
            await _invoker(
                serviceHandle.Instance,
                requestStream,
                responseStream,
                resolvedContext);
        }
        finally
        {
            if (serviceHandle.Instance != null)
            {
                await ServiceActivator.ReleaseAsync(serviceHandle);
            }
        }
    }

    /// <summary>
    /// Invoke the duplex streaming method with the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="serverCallContext">The <see cref="ServerCallContext"/>.</param>
    /// <param name="requestStream">The <typeparamref name="TRequest"/> reader.</param>
    /// <param name="responseStream">The <typeparamref name="TResponse"/> writer.</param>
    /// <returns>A <see cref="Task{TResponse}"/> that represents the asynchronous method.</returns>
    public async Task Invoke(HttpContext httpContext, ServerCallContext serverCallContext, IAsyncStreamReader<TRequest> requestStream, IServerStreamWriter<TResponse> responseStream)
    {
        if (_pipelineInvoker == null)
        {
            GrpcActivatorHandle<TService> serviceHandle = default;
            try
            {
                serviceHandle = CreateServiceHandle(httpContext);
                await _invoker(
                    serviceHandle.Instance,
                    requestStream,
                    responseStream,
                    serverCallContext);
            }
            finally
            {
                if (serviceHandle.Instance != null)
                {
                    await ServiceActivator.ReleaseAsync(serviceHandle);
                }
            }
        }
        else
        {
            await _pipelineInvoker(
                requestStream,
                responseStream,
                serverCallContext);
        }
    }
}
