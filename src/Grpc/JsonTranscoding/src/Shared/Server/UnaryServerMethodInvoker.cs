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
using System.Runtime.ExceptionServices;
using Grpc.AspNetCore.Server;
using Grpc.AspNetCore.Server.Model;
using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace Grpc.Shared.Server;

/// <summary>
/// Unary server method invoker.
/// </summary>
/// <typeparam name="TService">Service type for this method.</typeparam>
/// <typeparam name="TRequest">Request message type for this method.</typeparam>
/// <typeparam name="TResponse">Response message type for this method.</typeparam>
internal sealed class UnaryServerMethodInvoker<[DynamicallyAccessedMembers(ServerDynamicAccessConstants.ServiceAccessibility)] TService, TRequest, TResponse> : ServerMethodInvokerBase<TService, TRequest, TResponse>
    where TRequest : class
    where TResponse : class
    where TService : class
{
    private readonly UnaryServerMethod<TService, TRequest, TResponse> _invoker;
    private readonly UnaryServerMethod<TRequest, TResponse>? _pipelineInvoker;

    /// <summary>
    /// Creates a new instance of <see cref="UnaryServerMethodInvoker{TService, TRequest, TResponse}"/>.
    /// </summary>
    /// <param name="invoker">The unary method to invoke.</param>
    /// <param name="method">The description of the gRPC method.</param>
    /// <param name="options">The options used to execute the method.</param>
    /// <param name="serviceActivator">The service activator used to create service instances.</param>
    /// <param name="interceptorActivators">The interceptor activators used to create interceptor instances.</param>
    public UnaryServerMethodInvoker(
        UnaryServerMethod<TService, TRequest, TResponse> invoker,
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
            _pipelineInvoker = interceptorPipeline.UnaryPipeline(ResolvedInterceptorInvoker);
        }
    }

    private async Task<TResponse> ResolvedInterceptorInvoker(TRequest resolvedRequest, ServerCallContext resolvedContext)
    {
        GrpcActivatorHandle<TService> serviceHandle = default;
        try
        {
            serviceHandle = CreateServiceHandle(resolvedContext);
            return await _invoker(serviceHandle.Instance, resolvedRequest, resolvedContext);
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
    /// Invoke the unary method with the specified <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <param name="serverCallContext">The <see cref="ServerCallContext"/>.</param>
    /// <param name="request">The <typeparamref name="TRequest"/> message.</param>
    /// <returns>A <see cref="Task{TResponse}"/> that represents the asynchronous method. The <see cref="Task{TResponse}.Result"/>
    /// property returns the <typeparamref name="TResponse"/> message.</returns>
    public Task<TResponse> Invoke(HttpContext httpContext, ServerCallContext serverCallContext, TRequest request)
    {
        if (_pipelineInvoker == null)
        {
            GrpcActivatorHandle<TService> serviceHandle = default;
            Task<TResponse>? invokerTask = null;
            try
            {
                serviceHandle = CreateServiceHandle(httpContext);
                invokerTask = _invoker(
                    serviceHandle.Instance,
                    request,
                    serverCallContext);
            }
            catch (Exception ex)
            {
                // Invoker calls user code. User code may throw an exception instead
                // of a faulted task. We need to catch the exception, ensure cleanup
                // runs and convert exception into a faulted task.
                if (serviceHandle.Instance != null)
                {
                    var releaseTask = ServiceActivator.ReleaseAsync(serviceHandle);
                    if (!releaseTask.IsCompletedSuccessfully)
                    {
                        // Capture the current exception state so we can rethrow it after awaiting
                        // with the same stack trace.
                        var exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
                        return AwaitServiceReleaseAndThrow(releaseTask, exceptionDispatchInfo);
                    }
                }

                return Task.FromException<TResponse>(ex);
            }

            if (invokerTask.IsCompletedSuccessfully && serviceHandle.Instance != null)
            {
                var releaseTask = ServiceActivator.ReleaseAsync(serviceHandle);
                if (!releaseTask.IsCompletedSuccessfully)
                {
                    return AwaitServiceReleaseAndReturn(invokerTask.Result, serviceHandle);
                }

                return invokerTask;
            }

            return AwaitInvoker(invokerTask, serviceHandle);
        }
        else
        {
            return _pipelineInvoker(
                request,
                serverCallContext);
        }
    }

    private async Task<TResponse> AwaitInvoker(Task<TResponse> invokerTask, GrpcActivatorHandle<TService> serviceHandle)
    {
        try
        {
            return await invokerTask;
        }
        finally
        {
            if (serviceHandle.Instance != null)
            {
                await ServiceActivator.ReleaseAsync(serviceHandle);
            }
        }
    }

    private static async Task<TResponse> AwaitServiceReleaseAndThrow(ValueTask releaseTask, ExceptionDispatchInfo ex)
    {
        await releaseTask;
        ex.Throw();

        // Should never reach here
        return null;
    }

    private async Task<TResponse> AwaitServiceReleaseAndReturn(TResponse invokerResult, GrpcActivatorHandle<TService> serviceHandle)
    {
        await ServiceActivator.ReleaseAsync(serviceHandle);
        return invokerResult;
    }
}
