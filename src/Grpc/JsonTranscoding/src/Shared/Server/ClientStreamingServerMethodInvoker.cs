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
/// Client streaming server method invoker.
/// </summary>
/// <typeparam name="TService">Service type for this method.</typeparam>
/// <typeparam name="TRequest">Request message type for this method.</typeparam>
/// <typeparam name="TResponse">Response message type for this method.</typeparam>
internal sealed class ClientStreamingServerMethodInvoker<[DynamicallyAccessedMembers(ServerDynamicAccessConstants.ServiceAccessibility)] TService, TRequest, TResponse> : ServerMethodInvokerBase<TService, TRequest, TResponse>
    where TRequest : class
    where TResponse : class
    where TService : class
{
    private readonly ClientStreamingServerMethod<TService, TRequest, TResponse> _invoker;
    private readonly ClientStreamingServerMethod<TRequest, TResponse>? _pipelineInvoker;

    /// <summary>
    /// Creates a new instance of <see cref="ClientStreamingServerMethodInvoker{TService, TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="invoker">The client streaming method to invoke.</param>
    /// <param name="method">The description of the gRPC method.</param>
    /// <param name="options">The options used to execute the method.</param>
    /// <param name="serviceActivator">The service activator used to create service instances.</param>
    /// <param name="interceptorActivators">The interceptor activators used to create interceptor instances.</param>
    public ClientStreamingServerMethodInvoker(
        ClientStreamingServerMethod<TService, TRequest, TResponse> invoker,
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
            _pipelineInvoker = interceptorPipeline.ClientStreamingPipeline(ResolvedInterceptorInvoker);
        }
    }

    private async Task<TResponse> ResolvedInterceptorInvoker(IAsyncStreamReader<TRequest> requestStream, ServerCallContext resolvedContext)
    {
        GrpcActivatorHandle<TService> serviceHandle = default;
        try
        {
            serviceHandle = CreateServiceHandle(resolvedContext);
            return await _invoker(
                serviceHandle.Instance,
                requestStream,
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
    /// Invoke the client streaming method with the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="serverCallContext">The <see cref="ServerCallContext"/>.</param>
    /// <param name="requestStream">The <typeparamref name="TRequest"/> reader.</param>
    /// <returns>A <see cref="Task{TResponse}"/> that represents the asynchronous method. The <see cref="Task{TResponse}.Result"/>
    /// property returns the <typeparamref name="TResponse"/> message.</returns>
    public async Task<TResponse> Invoke(HttpContext httpContext, ServerCallContext serverCallContext, IAsyncStreamReader<TRequest> requestStream)
    {
        if (_pipelineInvoker == null)
        {
            GrpcActivatorHandle<TService> serviceHandle = default;
            try
            {
                serviceHandle = CreateServiceHandle(httpContext);
                return await _invoker(
                    serviceHandle.Instance,
                    requestStream,
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
            return await _pipelineInvoker(
                requestStream,
                serverCallContext);
        }
    }
}
