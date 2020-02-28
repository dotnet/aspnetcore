// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    internal partial class DefaultHubDispatcher<THub> : HubDispatcher<THub> where THub : Hub
    {
        private readonly Dictionary<string, HubMethodDescriptor> _methods = new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<THub> _hubContext;
        private readonly ILogger<HubDispatcher<THub>> _logger;
        private readonly bool _enableDetailedErrors;

        public DefaultHubDispatcher(IServiceScopeFactory serviceScopeFactory, IHubContext<THub> hubContext, bool enableDetailedErrors, ILogger<DefaultHubDispatcher<THub>> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
            _enableDetailedErrors = enableDetailedErrors;
            _logger = logger;
            DiscoverHubMethods();
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            IServiceScope scope = null;

            try
            {
                scope = _serviceScopeFactory.CreateScope();

                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();
                try
                {
                    InitializeHub(hub, connection);
                    await hub.OnConnectedAsync();
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
            finally
            {
                await scope.DisposeAsync();
            }
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception)
        {
            IServiceScope scope = null;

            try
            {
                scope = _serviceScopeFactory.CreateScope();

                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();
                try
                {
                    InitializeHub(hub, connection);
                    await hub.OnDisconnectedAsync(exception);
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
            finally
            {
                await scope.DisposeAsync();
            }
        }

        public override Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
        {
            // Messages are dispatched sequentially and will stop other messages from being processed until they complete.
            // Streaming methods will run sequentially until they start streaming, then they will fire-and-forget allowing other messages to run.

            switch (hubMessage)
            {
                case InvocationBindingFailureMessage bindingFailureMessage:
                    return ProcessInvocationBindingFailure(connection, bindingFailureMessage);

                case StreamBindingFailureMessage bindingFailureMessage:
                    return ProcessStreamBindingFailure(connection, bindingFailureMessage);

                case InvocationMessage invocationMessage:
                    Log.ReceivedHubInvocation(_logger, invocationMessage);
                    return ProcessInvocation(connection, invocationMessage, isStreamResponse: false);

                case StreamInvocationMessage streamInvocationMessage:
                    Log.ReceivedStreamHubInvocation(_logger, streamInvocationMessage);
                    return ProcessInvocation(connection, streamInvocationMessage, isStreamResponse: true);

                case CancelInvocationMessage cancelInvocationMessage:
                    // Check if there is an associated active stream and cancel it if it exists.
                    // The cts will be removed when the streaming method completes executing
                    if (connection.ActiveRequestCancellationSources.TryGetValue(cancelInvocationMessage.InvocationId, out var cts))
                    {
                        Log.CancelStream(_logger, cancelInvocationMessage.InvocationId);
                        cts.Cancel();
                    }
                    else
                    {
                        // Stream can be canceled on the server while client is canceling stream.
                        Log.UnexpectedCancel(_logger);
                    }
                    break;

                case PingMessage _:
                    connection.StartClientTimeout();
                    break;

                case StreamItemMessage streamItem:
                    return ProcessStreamItem(connection, streamItem);

                case CompletionMessage streamCompleteMessage:
                    // closes channels, removes from Lookup dict
                    // user's method can see the channel is complete and begin wrapping up
                    if (connection.StreamTracker.TryComplete(streamCompleteMessage))
                    {
                        Log.CompletingStream(_logger, streamCompleteMessage);
                    }
                    else
                    {
                        Log.UnexpectedStreamCompletion(_logger);
                    }
                    break;

                // Other kind of message we weren't expecting
                default:
                    Log.UnsupportedMessageReceived(_logger, hubMessage.GetType().FullName);
                    throw new NotSupportedException($"Received unsupported message: {hubMessage}");
            }

            return Task.CompletedTask;
        }

        private Task ProcessInvocationBindingFailure(HubConnectionContext connection, InvocationBindingFailureMessage bindingFailureMessage)
        {
            Log.InvalidHubParameters(_logger, bindingFailureMessage.Target, bindingFailureMessage.BindingFailure.SourceException);

            var errorMessage = ErrorMessageHelper.BuildErrorMessage($"Failed to invoke '{bindingFailureMessage.Target}' due to an error on the server.",
                bindingFailureMessage.BindingFailure.SourceException, _enableDetailedErrors);
            return SendInvocationError(bindingFailureMessage.InvocationId, connection, errorMessage);
        }

        private Task ProcessStreamBindingFailure(HubConnectionContext connection, StreamBindingFailureMessage bindingFailureMessage)
        {
            var errorString = ErrorMessageHelper.BuildErrorMessage(
                "Failed to bind Stream message.",
                bindingFailureMessage.BindingFailure.SourceException, _enableDetailedErrors);

            var message = CompletionMessage.WithError(bindingFailureMessage.Id, errorString);
            Log.ClosingStreamWithBindingError(_logger, message);

            // ignore failure, it means the client already completed the stream or the stream never existed on the server
            connection.StreamTracker.TryComplete(message);

            // TODO: Send stream completion message to client when we add it

            return Task.CompletedTask;
        }

        private Task ProcessStreamItem(HubConnectionContext connection, StreamItemMessage message)
        {
            if (!connection.StreamTracker.TryProcessItem(message, out var processTask))
            {
                Log.UnexpectedStreamItem(_logger);
                return Task.CompletedTask;
            }

            Log.ReceivedStreamItem(_logger, message);
            return processTask;
        }

        private Task ProcessInvocation(HubConnectionContext connection,
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamResponse)
        {
            if (!_methods.TryGetValue(hubMethodInvocationMessage.Target, out var descriptor))
            {
                // Send an error to the client. Then let the normal completion process occur
                Log.UnknownHubMethod(_logger, hubMethodInvocationMessage.Target);
                return connection.WriteAsync(CompletionMessage.WithError(
                    hubMethodInvocationMessage.InvocationId, $"Unknown hub method '{hubMethodInvocationMessage.Target}'")).AsTask();
            }
            else
            {
                bool isStreamCall = descriptor.StreamingParameters != null;
                return Invoke(descriptor, connection, hubMethodInvocationMessage, isStreamResponse, isStreamCall);
            }
        }

        private async Task Invoke(HubMethodDescriptor descriptor, HubConnectionContext connection,
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamResponse, bool isStreamCall)
        {
            var methodExecutor = descriptor.MethodExecutor;

            var disposeScope = true;
            var scope = _serviceScopeFactory.CreateScope();
            IHubActivator<THub> hubActivator = null;
            THub hub = null;
            try
            {
                if (!await IsHubMethodAuthorized(scope.ServiceProvider, connection, descriptor.Policies, descriptor.MethodExecutor.MethodInfo.Name, hubMethodInvocationMessage.Arguments))
                {
                    Log.HubMethodNotAuthorized(_logger, hubMethodInvocationMessage.Target);
                    await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                        $"Failed to invoke '{hubMethodInvocationMessage.Target}' because user is unauthorized");
                    return;
                }

                if (!await ValidateInvocationMode(descriptor, isStreamResponse, hubMethodInvocationMessage, connection))
                {
                    return;
                }

                hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                hub = hubActivator.Create();

                try
                {
                    var clientStreamLength = hubMethodInvocationMessage.StreamIds?.Length ?? 0;
                    var serverStreamLength = descriptor.StreamingParameters?.Count ?? 0;
                    if (clientStreamLength != serverStreamLength)
                    {
                        var ex = new HubException($"Client sent {clientStreamLength} stream(s), Hub method expects {serverStreamLength}.");
                        Log.InvalidHubParameters(_logger, hubMethodInvocationMessage.Target, ex);
                        await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                            ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors));
                        return;
                    }

                    InitializeHub(hub, connection);
                    Task invocation = null;

                    CancellationTokenSource cts = null;
                    var arguments = hubMethodInvocationMessage.Arguments;
                    if (descriptor.HasSyntheticArguments)
                    {
                        // In order to add the synthetic arguments we need a new array because the invocation array is too small (it doesn't know about synthetic arguments)
                        arguments = new object[descriptor.OriginalParameterTypes.Count];

                        var streamPointer = 0;
                        var hubInvocationArgumentPointer = 0;
                        for (var parameterPointer = 0; parameterPointer < arguments.Length; parameterPointer++)
                        {
                            if (hubMethodInvocationMessage.Arguments.Length > hubInvocationArgumentPointer &&
                                (hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer] == null ||
                                descriptor.OriginalParameterTypes[parameterPointer].IsAssignableFrom(hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer].GetType())))
                            {
                                // The types match so it isn't a synthetic argument, just copy it into the arguments array
                                arguments[parameterPointer] = hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer];
                                hubInvocationArgumentPointer++;
                            }
                            else
                            {
                                if (descriptor.OriginalParameterTypes[parameterPointer] == typeof(CancellationToken))
                                {
                                    cts = CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted);
                                    arguments[parameterPointer] = cts.Token;
                                }
                                else if (isStreamCall && ReflectionHelper.IsStreamingType(descriptor.OriginalParameterTypes[parameterPointer], mustBeDirectType: true))
                                {
                                    Log.StartingParameterStream(_logger, hubMethodInvocationMessage.StreamIds[streamPointer]);
                                    var itemType = descriptor.StreamingParameters[streamPointer];
                                    arguments[parameterPointer] = connection.StreamTracker.AddStream(hubMethodInvocationMessage.StreamIds[streamPointer],
                                        itemType, descriptor.OriginalParameterTypes[parameterPointer]);

                                    streamPointer++;
                                }
                                else
                                {
                                    // This should never happen
                                    Debug.Assert(false, $"Failed to bind argument of type '{descriptor.OriginalParameterTypes[parameterPointer].Name}' for hub method '{methodExecutor.MethodInfo.Name}'.");
                                }
                            }
                        }
                    }

                    if (isStreamResponse)
                    {
                        var result = await ExecuteHubMethod(methodExecutor, hub, arguments);

                        if (result == null)
                        {
                            Log.InvalidReturnValueFromStreamingMethod(_logger, methodExecutor.MethodInfo.Name);
                            await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                                $"The value returned by the streaming method '{methodExecutor.MethodInfo.Name}' is not a ChannelReader<> or IAsyncEnumerable<>.");
                            return;
                        }

                        cts = cts ?? CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted);
                        connection.ActiveRequestCancellationSources.TryAdd(hubMethodInvocationMessage.InvocationId, cts);
                        var enumerable = descriptor.FromReturnedStream(result, cts.Token);

                        Log.StreamingResult(_logger, hubMethodInvocationMessage.InvocationId, methodExecutor);
                        _ = StreamResultsAsync(hubMethodInvocationMessage.InvocationId, connection, enumerable, scope, hubActivator, hub, cts, hubMethodInvocationMessage);
                    }

                    else
                    {
                        // Invoke or Send
                        async Task ExecuteInvocation()
                        {
                            object result;
                            try
                            {
                                result = await ExecuteHubMethod(methodExecutor, hub, arguments);
                                Log.SendingResult(_logger, hubMethodInvocationMessage.InvocationId, methodExecutor);
                            }
                            catch (Exception ex)
                            {
                                Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                                await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                                    ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors));
                                return;
                            }
                            finally
                            {
                                // Stream response handles cleanup in StreamResultsAsync
                                // And normal invocations handle cleanup below in the finally
                                if (isStreamCall)
                                {
                                    await CleanupInvocation(connection, hubMethodInvocationMessage, hubActivator, hub, scope);
                                }
                            }

                            // No InvocationId - Send Async, no response expected
                            if (!string.IsNullOrEmpty(hubMethodInvocationMessage.InvocationId))
                            {
                                // Invoke Async, one reponse expected
                                await connection.WriteAsync(CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, result));
                            }
                        }
                        invocation = ExecuteInvocation();
                    }

                    if (isStreamCall || isStreamResponse)
                    {
                        // don't await streaming invocations
                        // leave them running in the background, allowing dispatcher to process other messages between streaming items
                        disposeScope = false;
                    }
                    else
                    {
                        // complete the non-streaming calls now
                        await invocation;
                    }
                }
                catch (TargetInvocationException ex)
                {
                    Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                        ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex.InnerException, _enableDetailedErrors));
                }
                catch (Exception ex)
                {
                    Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                        ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors));
                }
            }
            finally
            {
                if (disposeScope)
                {
                    await CleanupInvocation(connection, hubMethodInvocationMessage, hubActivator, hub, scope);
                }
            }
        }

        private ValueTask CleanupInvocation(HubConnectionContext connection, HubMethodInvocationMessage hubMessage, IHubActivator<THub> hubActivator,
            THub hub, IServiceScope scope)
        {
            if (hubMessage.StreamIds != null)
            {
                foreach (var stream in hubMessage.StreamIds)
                {
                    connection.StreamTracker.TryComplete(CompletionMessage.Empty(stream));
                }
            }

            hubActivator?.Release(hub);

            return scope.DisposeAsync();
        }

        private async Task StreamResultsAsync(string invocationId, HubConnectionContext connection, IAsyncEnumerable<object> enumerable, IServiceScope scope,
            IHubActivator<THub> hubActivator, THub hub, CancellationTokenSource streamCts, HubMethodInvocationMessage hubMethodInvocationMessage)
        {
            string error = null;

            try
            {
                await foreach (var streamItem in enumerable)
                {
                    // Send the stream item
                    await connection.WriteAsync(new StreamItemMessage(invocationId, streamItem));
                }
            }
            catch (ChannelClosedException ex)
            {
                // If the channel closes from an exception in the streaming method, grab the innerException for the error from the streaming method
                error = ErrorMessageHelper.BuildErrorMessage("An error occurred on the server while streaming results.", ex.InnerException ?? ex, _enableDetailedErrors);
            }
            catch (Exception ex)
            {
                // If the streaming method was canceled we don't want to send a HubException message - this is not an error case
                if (!(ex is OperationCanceledException && connection.ActiveRequestCancellationSources.TryGetValue(invocationId, out var cts)
                    && cts.IsCancellationRequested))
                {
                    error = ErrorMessageHelper.BuildErrorMessage("An error occurred on the server while streaming results.", ex, _enableDetailedErrors);
                }
            }
            finally
            {
                await CleanupInvocation(connection, hubMethodInvocationMessage, hubActivator, hub, scope);

                // Dispose the linked CTS for the stream.
                streamCts.Dispose();

                await connection.WriteAsync(CompletionMessage.WithError(invocationId, error));

                if (connection.ActiveRequestCancellationSources.TryRemove(invocationId, out var cts))
                {
                    cts.Dispose();
                }
            }
        }

        private static async Task<object> ExecuteHubMethod(ObjectMethodExecutor methodExecutor, THub hub, object[] arguments)
        {
            if (methodExecutor.IsMethodAsync)
            {
                if (methodExecutor.MethodReturnType == typeof(Task))
                {
                    await (Task)methodExecutor.Execute(hub, arguments);
                }
                else
                {
                    return await methodExecutor.ExecuteAsync(hub, arguments);
                }
            }
            else
            {
                return methodExecutor.Execute(hub, arguments);
            }

            return null;
        }

        private async Task SendInvocationError(string invocationId,
            HubConnectionContext connection, string errorMessage)
        {
            if (string.IsNullOrEmpty(invocationId))
            {
                return;
            }

            await connection.WriteAsync(CompletionMessage.WithError(invocationId, errorMessage));
        }

        private void InitializeHub(THub hub, HubConnectionContext connection)
        {
            hub.Clients = new HubCallerClients(_hubContext.Clients, connection.ConnectionId);
            hub.Context = connection.HubCallerContext;
            hub.Groups = _hubContext.Groups;
        }

        private Task<bool> IsHubMethodAuthorized(IServiceProvider provider, HubConnectionContext hubConnectionContext, IList<IAuthorizeData> policies, string hubMethodName, object[] hubMethodArguments)
        {
            // If there are no policies we don't need to run auth
            if (policies.Count == 0)
            {
                return TaskCache.True;
            }

            return IsHubMethodAuthorizedSlow(provider, hubConnectionContext.User, policies, new HubInvocationContext(hubConnectionContext.HubCallerContext, typeof(THub), hubMethodName, hubMethodArguments));
        }

        private static async Task<bool> IsHubMethodAuthorizedSlow(IServiceProvider provider, ClaimsPrincipal principal, IList<IAuthorizeData> policies, HubInvocationContext resource)
        {
            var authService = provider.GetRequiredService<IAuthorizationService>();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            // AuthorizationPolicy.CombineAsync only returns null if there are no policies and we check that above
            Debug.Assert(authorizePolicy != null);

            var authorizationResult = await authService.AuthorizeAsync(principal, resource, authorizePolicy);
            // Only check authorization success, challenge or forbid wouldn't make sense from a hub method invocation
            return authorizationResult.Succeeded;
        }

        private async Task<bool> ValidateInvocationMode(HubMethodDescriptor hubMethodDescriptor, bool isStreamResponse,
            HubMethodInvocationMessage hubMethodInvocationMessage, HubConnectionContext connection)
        {
            if (hubMethodDescriptor.IsStreamResponse && !isStreamResponse)
            {
                // Non-null/empty InvocationId? Blocking
                if (!string.IsNullOrEmpty(hubMethodInvocationMessage.InvocationId))
                {
                    Log.StreamingMethodCalledWithInvoke(_logger, hubMethodInvocationMessage);
                    await connection.WriteAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                        $"The client attempted to invoke the streaming '{hubMethodInvocationMessage.Target}' method with a non-streaming invocation."));
                }

                return false;
            }

            if (!hubMethodDescriptor.IsStreamResponse && isStreamResponse)
            {
                Log.NonStreamingMethodCalledWithStream(_logger, hubMethodInvocationMessage);
                await connection.WriteAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                    $"The client attempted to invoke the non-streaming '{hubMethodInvocationMessage.Target}' method with a streaming invocation."));

                return false;
            }

            return true;
        }

        private void DiscoverHubMethods()
        {
            var hubType = typeof(THub);
            var hubTypeInfo = hubType.GetTypeInfo();
            var hubName = hubType.Name;

            foreach (var methodInfo in HubReflectionHelper.GetHubMethods(hubType))
            {
                if (methodInfo.IsGenericMethod)
                {
                    throw new NotSupportedException($"Method '{methodInfo.Name}' is a generic method which is not supported on a Hub.");
                }

                var methodName =
                    methodInfo.GetCustomAttribute<HubMethodNameAttribute>()?.Name ??
                    methodInfo.Name;

                if (_methods.ContainsKey(methodName))
                {
                    throw new NotSupportedException($"Duplicate definitions of '{methodName}'. Overloading is not supported.");
                }

                var executor = ObjectMethodExecutor.Create(methodInfo, hubTypeInfo);
                var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
                _methods[methodName] = new HubMethodDescriptor(executor, authorizeAttributes);

                Log.HubMethodBound(_logger, hubName, methodName);
            }
        }

        public override IReadOnlyList<Type> GetParameterTypes(string methodName)
        {
            if (!_methods.TryGetValue(methodName, out var descriptor))
            {
                throw new HubException("Method does not exist.");
            }
            return descriptor.ParameterTypes;
        }
    }
}
