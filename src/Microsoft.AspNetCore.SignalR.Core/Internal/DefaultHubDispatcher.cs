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
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.SignalR.Internal
{
    public partial class DefaultHubDispatcher<THub> : HubDispatcher<THub> where THub : Hub
    {
        private readonly Dictionary<string, HubMethodDescriptor> _methods = new Dictionary<string, HubMethodDescriptor>(StringComparer.OrdinalIgnoreCase);
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IHubContext<THub> _hubContext;
        private readonly ILogger<HubDispatcher<THub>> _logger;
        private readonly bool _enableDetailedErrors;

        public DefaultHubDispatcher(IServiceScopeFactory serviceScopeFactory, IHubContext<THub> hubContext, IOptions<HubOptions<THub>> hubOptions,
            IOptions<HubOptions> globalHubOptions, ILogger<DefaultHubDispatcher<THub>> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _hubContext = hubContext;
            _enableDetailedErrors = hubOptions.Value.EnableDetailedErrors ?? globalHubOptions.Value.EnableDetailedErrors ?? false;
            _logger = logger;
            DiscoverHubMethods();
        }

        public override async Task OnConnectedAsync(HubConnectionContext connection)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
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
        }

        public override async Task OnDisconnectedAsync(HubConnectionContext connection, Exception exception)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
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
        }

        public override async Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
        {
            switch (hubMessage)
            {
                case InvocationMessage invocationMessage:
                    Log.ReceivedHubInvocation(_logger, invocationMessage);
                    await ProcessInvocation(connection, invocationMessage, isStreamedInvocation: false);
                    break;

                case StreamInvocationMessage streamInvocationMessage:
                    Log.ReceivedStreamHubInvocation(_logger, streamInvocationMessage);
                    await ProcessInvocation(connection, streamInvocationMessage, isStreamedInvocation: true);
                    break;

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
                    // We don't care about pings
                    break;

                // Other kind of message we weren't expecting
                default:
                    Log.UnsupportedMessageReceived(_logger, hubMessage.GetType().FullName);
                    throw new NotSupportedException($"Received unsupported message: {hubMessage}");
            }
        }

        public override Type GetReturnType(string invocationId)
        {
            return typeof(object);
        }

        public override IReadOnlyList<Type> GetParameterTypes(string methodName)
        {
            if (!_methods.TryGetValue(methodName, out var descriptor))
            {
                return Type.EmptyTypes;
            }
            return descriptor.ParameterTypes;
        }

        private async Task ProcessInvocation(HubConnectionContext connection,
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamedInvocation)
        {
            try
            {
                // If an unexpected exception occurs then we want to kill the entire connection
                // by ending the processing loop
                if (!_methods.TryGetValue(hubMethodInvocationMessage.Target, out var descriptor))
                {
                    // Send an error to the client. Then let the normal completion process occur
                    Log.UnknownHubMethod(_logger, hubMethodInvocationMessage.Target);
                    await connection.WriteAsync(CompletionMessage.WithError(
                        hubMethodInvocationMessage.InvocationId, $"Unknown hub method '{hubMethodInvocationMessage.Target}'"));
                }
                else
                {
                    await Invoke(descriptor, connection, hubMethodInvocationMessage, isStreamedInvocation);
                }
            }
            catch (Exception ex)
            {
                // Abort the entire connection if the invocation fails in an unexpected way
                connection.Abort(ex);
            }
        }

        private async Task Invoke(HubMethodDescriptor descriptor, HubConnectionContext connection,
            HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamedInvocation)
        {
            var methodExecutor = descriptor.MethodExecutor;

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                if (!await IsHubMethodAuthorized(scope.ServiceProvider, connection.User, descriptor.Policies))
                {
                    Log.HubMethodNotAuthorized(_logger, hubMethodInvocationMessage.Target);
                    await SendInvocationError(hubMethodInvocationMessage, connection,
                        $"Failed to invoke '{hubMethodInvocationMessage.Target}' because user is unauthorized");
                    return;
                }

                if (!await ValidateInvocationMode(descriptor, isStreamedInvocation, hubMethodInvocationMessage, connection))
                {
                    return;
                }

                if (hubMethodInvocationMessage.ArgumentBindingException != null)
                {
                    Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, hubMethodInvocationMessage.ArgumentBindingException);
                    var errorMessage = ErrorMessageHelper.BuildErrorMessage($"Failed to invoke '{hubMethodInvocationMessage.Target}' due to an error on the server.",
                        hubMethodInvocationMessage.ArgumentBindingException, _enableDetailedErrors);
                    await SendInvocationError(hubMethodInvocationMessage, connection, errorMessage);
                    return;
                }

                var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
                var hub = hubActivator.Create();

                try
                {
                    InitializeHub(hub, connection);

                    var result = await ExecuteHubMethod(methodExecutor, hub, hubMethodInvocationMessage.Arguments);

                    if (isStreamedInvocation)
                    {
                        if (!TryGetStreamingEnumerator(connection, hubMethodInvocationMessage.InvocationId, descriptor, result, out var enumerator, out var streamCts))
                        {
                            Log.InvalidReturnValueFromStreamingMethod(_logger, methodExecutor.MethodInfo.Name);

                            await SendInvocationError(hubMethodInvocationMessage, connection,
                                $"The value returned by the streaming method '{methodExecutor.MethodInfo.Name}' is not a ChannelReader<>.");
                            return;
                        }

                        Log.StreamingResult(_logger, hubMethodInvocationMessage.InvocationId, methodExecutor);
                        await StreamResultsAsync(hubMethodInvocationMessage.InvocationId, connection, enumerator, streamCts);
                    }
                    // Non-empty/null InvocationId ==> Blocking invocation that needs a response
                    else if (!string.IsNullOrEmpty(hubMethodInvocationMessage.InvocationId))
                    {
                        Log.SendingResult(_logger, hubMethodInvocationMessage.InvocationId, methodExecutor);
                        await connection.WriteAsync(CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, result));
                    }
                }
                catch (TargetInvocationException ex)
                {
                    Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage, connection,
                        ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex.InnerException, _enableDetailedErrors));
                }
                catch (Exception ex)
                {
                    Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                    await SendInvocationError(hubMethodInvocationMessage, connection,
                        ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors));
                }
                finally
                {
                    hubActivator.Release(hub);
                }
            }
        }

        private async Task StreamResultsAsync(string invocationId, HubConnectionContext connection, IAsyncEnumerator<object> enumerator, CancellationTokenSource streamCts)
        {
            string error = null;

            try
            {
                while (await enumerator.MoveNextAsync())
                {
                    // Send the stream item
                    await connection.WriteAsync(new StreamItemMessage(invocationId, enumerator.Current));
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
                (enumerator as IDisposable)?.Dispose();

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

        private async Task SendInvocationError(HubMethodInvocationMessage hubMethodInvocationMessage,
            HubConnectionContext connection, string errorMessage)
        {
            if (string.IsNullOrEmpty(hubMethodInvocationMessage.InvocationId))
            {
                return;
            }

            await connection.WriteAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId, errorMessage));
        }

        private void InitializeHub(THub hub, HubConnectionContext connection)
        {
            hub.Clients = new HubCallerClients(_hubContext.Clients, connection.ConnectionId);
            hub.Context = new DefaultHubCallerContext(connection);
            hub.Groups = _hubContext.Groups;
        }

        private async Task<bool> IsHubMethodAuthorized(IServiceProvider provider, ClaimsPrincipal principal, IList<IAuthorizeData> policies)
        {
            // If there are no policies we don't need to run auth
            if (!policies.Any())
            {
                return true;
            }

            var authService = provider.GetRequiredService<IAuthorizationService>();
            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();

            var authorizePolicy = await AuthorizationPolicy.CombineAsync(policyProvider, policies);
            // AuthorizationPolicy.CombineAsync only returns null if there are no policies and we check that above
            Debug.Assert(authorizePolicy != null);

            var authorizationResult = await authService.AuthorizeAsync(principal, authorizePolicy);
            // Only check authorization success, challenge or forbid wouldn't make sense from a hub method invocation
            return authorizationResult.Succeeded;
        }

        private async Task<bool> ValidateInvocationMode(HubMethodDescriptor hubMethodDescriptor, bool isStreamedInvocation,
            HubMethodInvocationMessage hubMethodInvocationMessage, HubConnectionContext connection)
        {
            if (hubMethodDescriptor.IsStreamable && !isStreamedInvocation)
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

            if (!hubMethodDescriptor.IsStreamable && isStreamedInvocation)
            {
                Log.NonStreamingMethodCalledWithStream(_logger, hubMethodInvocationMessage);
                await connection.WriteAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId,
                    $"The client attempted to invoke the non-streaming '{hubMethodInvocationMessage.Target}' method with a streaming invocation."));

                return false;
            }

            return true;
        }

        private bool TryGetStreamingEnumerator(HubConnectionContext connection, string invocationId, HubMethodDescriptor hubMethodDescriptor, object result, out IAsyncEnumerator<object> enumerator, out CancellationTokenSource streamCts)
        {
            if (result != null)
            {
                if (hubMethodDescriptor.IsChannel)
                {
                    streamCts = CreateCancellation();
                    enumerator = hubMethodDescriptor.FromChannel(result, streamCts.Token);
                    return true;
                }
            }

            streamCts = null;
            enumerator = null;
            return false;

            CancellationTokenSource CreateCancellation()
            {
                var userCts = new CancellationTokenSource();
                connection.ActiveRequestCancellationSources.TryAdd(invocationId, userCts);

                return CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted, userCts.Token);
            }
        }

        private void DiscoverHubMethods()
        {
            var hubType = typeof(THub);
            var hubTypeInfo = hubType.GetTypeInfo();
            var hubName = hubType.Name;

            foreach (var methodInfo in HubReflectionHelper.GetHubMethods(hubType))
            {
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
    }
}
