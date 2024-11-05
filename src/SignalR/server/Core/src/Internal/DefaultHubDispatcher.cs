// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Internal;
using Microsoft.AspNetCore.Shared;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Log = Microsoft.AspNetCore.SignalR.Internal.DefaultHubDispatcherLog;

namespace Microsoft.AspNetCore.SignalR.Internal;

internal sealed partial class DefaultHubDispatcher<[DynamicallyAccessedMembers(Hub.DynamicallyAccessedMembers)] THub> : HubDispatcher<THub> where THub : Hub
{
    private static readonly string _fullHubName = typeof(THub).FullName ?? typeof(THub).Name;

    private readonly Dictionary<string, HubMethodDescriptor> _methods = new(StringComparer.OrdinalIgnoreCase);
    private readonly Utf8HashLookup _cachedMethodNames = new();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IHubContext<THub> _hubContext;
    private readonly ILogger<HubDispatcher<THub>> _logger;
    private readonly bool _enableDetailedErrors;
    private readonly Func<HubInvocationContext, ValueTask<object?>>? _invokeMiddleware;
    private readonly Func<HubLifetimeContext, Task>? _onConnectedMiddleware;
    private readonly Func<HubLifetimeContext, Exception?, Task>? _onDisconnectedMiddleware;
    private readonly HubLifetimeManager<THub> _hubLifetimeManager;

    [FeatureSwitchDefinition("Microsoft.AspNetCore.SignalR.Hub.IsCustomAwaitableSupported")]
    [FeatureGuard(typeof(RequiresDynamicCodeAttribute))]
    [FeatureGuard(typeof(RequiresUnreferencedCodeAttribute))]
    private static bool IsCustomAwaitableSupported { get; } =
        AppContext.TryGetSwitch("Microsoft.AspNetCore.SignalR.Hub.IsCustomAwaitableSupported", out bool customAwaitableSupport) ? customAwaitableSupport : true;

    public DefaultHubDispatcher(IServiceScopeFactory serviceScopeFactory, IHubContext<THub> hubContext, bool enableDetailedErrors,
        bool disableImplicitFromServiceParameters, ILogger<DefaultHubDispatcher<THub>> logger, List<IHubFilter>? hubFilters, HubLifetimeManager<THub> lifetimeManager)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _hubContext = hubContext;
        _enableDetailedErrors = enableDetailedErrors;
        _logger = logger;
        _hubLifetimeManager = lifetimeManager;
        DiscoverHubMethods(disableImplicitFromServiceParameters);

        var count = hubFilters?.Count ?? 0;
        if (count != 0)
        {
            _invokeMiddleware = (invocationContext) =>
            {
                var arguments = invocationContext.HubMethodArguments as object?[] ?? invocationContext.HubMethodArguments.ToArray();
                if (invocationContext.ObjectMethodExecutor != null)
                {
                    return ExecuteMethod(invocationContext.ObjectMethodExecutor, invocationContext.Hub, arguments);
                }
                return ExecuteMethod(invocationContext.HubMethod.Name, invocationContext.Hub, arguments);
            };

            _onConnectedMiddleware = (context) => context.Hub.OnConnectedAsync();
            _onDisconnectedMiddleware = (context, exception) => context.Hub.OnDisconnectedAsync(exception);

            for (var i = count - 1; i > -1; i--)
            {
                var resolvedFilter = hubFilters![i];
                var nextFilter = _invokeMiddleware;
                _invokeMiddleware = (context) => resolvedFilter.InvokeMethodAsync(context, nextFilter);

                var connectedFilter = _onConnectedMiddleware;
                _onConnectedMiddleware = (context) => resolvedFilter.OnConnectedAsync(context, connectedFilter);

                var disconnectedFilter = _onDisconnectedMiddleware;
                _onDisconnectedMiddleware = (context, exception) => resolvedFilter.OnDisconnectedAsync(context, exception, disconnectedFilter);
            }
        }
    }

    public override async Task OnConnectedAsync(HubConnectionContext connection)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
        var hub = hubActivator.Create();
        Activity? activity = null;
        try
        {
            // OnConnectedAsync won't work with client results (ISingleClientProxy.InvokeAsync)
            InitializeHub(hub, connection, invokeAllowed: false);

            activity = StartActivity(SignalRServerActivitySource.OnConnected, ActivityKind.Internal, linkedActivity: null, scope.ServiceProvider, nameof(hub.OnConnectedAsync), headers: null, _logger);

            if (_onConnectedMiddleware != null)
            {
                var context = new HubLifetimeContext(connection.HubCallerContext, scope.ServiceProvider, hub);
                await _onConnectedMiddleware(context);
            }
            else
            {
                await hub.OnConnectedAsync();
            }
        }
        catch (Exception ex)
        {
            SetActivityError(activity, ex);
            throw;
        }
        finally
        {
            activity?.Stop();
            hubActivator.Release(hub);
        }
    }

    public override async Task OnDisconnectedAsync(HubConnectionContext connection, Exception? exception)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();

        var hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
        var hub = hubActivator.Create();
        Activity? activity = null;
        try
        {
            InitializeHub(hub, connection);

            activity = StartActivity(SignalRServerActivitySource.OnDisconnected, ActivityKind.Internal, linkedActivity: null, scope.ServiceProvider, nameof(hub.OnDisconnectedAsync), headers: null, _logger);

            if (_onDisconnectedMiddleware != null)
            {
                var context = new HubLifetimeContext(connection.HubCallerContext, scope.ServiceProvider, hub);
                await _onDisconnectedMiddleware(context, exception);
            }
            else
            {
                await hub.OnDisconnectedAsync(exception);
            }
        }
        catch (Exception ex)
        {
            SetActivityError(activity, ex);
            throw;
        }
        finally
        {
            activity?.Stop();
            hubActivator.Release(hub);
        }
    }

    public override Task DispatchMessageAsync(HubConnectionContext connection, HubMessage hubMessage)
    {
        // Messages are dispatched sequentially and will stop other messages from being processed until they complete.
        // Streaming methods will run sequentially until they start streaming, then they will fire-and-forget allowing other messages to run.

        // With parallel invokes enabled, messages run sequentially until they go async and then the next message will be allowed to start running.

        if (!connection.ShouldProcessMessage(hubMessage))
        {
            Log.DroppingMessage(_logger, hubMessage.GetType().Name, (hubMessage as HubInvocationMessage)?.InvocationId ?? "(null)");
            return Task.CompletedTask;
        }

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
                if (connection.ActiveRequestCancellationSources.TryGetValue(cancelInvocationMessage.InvocationId!, out var cts))
                {
                    Log.CancelStream(_logger, cancelInvocationMessage.InvocationId!);
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

            case CompletionMessage completionMessage:
                // closes channels, removes from Lookup dict
                // user's method can see the channel is complete and begin wrapping up
                if (connection.StreamTracker.TryComplete(completionMessage))
                {
                    Log.CompletingStream(_logger, completionMessage);
                }
                // InvocationId is always required on CompletionMessage, it's nullable because of the base type
                else if (_hubLifetimeManager.TryGetReturnType(completionMessage.InvocationId!, out _))
                {
                    return _hubLifetimeManager.SetConnectionResultAsync(connection.ConnectionId, completionMessage);
                }
                else
                {
                    Log.UnexpectedCompletion(_logger, completionMessage.InvocationId!);
                }
                break;

            case AckMessage ackMessage:
                Log.ReceivedAckMessage(_logger, ackMessage.SequenceId);
                return connection.AckAsync(ackMessage);

            case SequenceMessage sequenceMessage:
                Log.ReceivedSequenceMessage(_logger, sequenceMessage.SequenceId);
                break;

            case CloseMessage closeMessage:
                connection.CloseMessage = closeMessage;
                connection.Abort();
                break;

            // Other kind of message we weren't expecting
            default:
                Log.UnsupportedMessageReceived(_logger, hubMessage.GetType().FullName!);
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
            Log.UnknownHubMethod(_logger, hubMethodInvocationMessage.Target);

            if (!string.IsNullOrEmpty(hubMethodInvocationMessage.InvocationId))
            {
                // Send an error to the client. Then let the normal completion process occur
                return connection.WriteAsync(CompletionMessage.WithError(
                    hubMethodInvocationMessage.InvocationId, $"Unknown hub method '{hubMethodInvocationMessage.Target}'")).AsTask();
            }
            else
            {
                return Task.CompletedTask;
            }
        }
        else
        {
            bool isStreamCall = descriptor.StreamingParameters != null;
            if (!isStreamCall && !isStreamResponse)
            {
                return connection.ActiveInvocationLimit.RunAsync(static state =>
                {
                    var (dispatcher, descriptor, connection, invocationMessage) = state;
                    return dispatcher.Invoke(descriptor, connection, invocationMessage, isStreamResponse: false, isStreamCall: false);
                }, (this, descriptor, connection, hubMethodInvocationMessage)).AsTask();
            }
            else
            {
                return Invoke(descriptor, connection, hubMethodInvocationMessage, isStreamResponse, isStreamCall);
            }
        }
    }

    private async Task<bool> Invoke(HubMethodDescriptor descriptor, HubConnectionContext connection,
        HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamResponse, bool isStreamCall)
    {
        var methodExecutor = descriptor.MethodExecutor;

        var wasSemaphoreReleased = false;
        var disposeScope = true;
        var scope = _serviceScopeFactory.CreateAsyncScope();
        IHubActivator<THub>? hubActivator = null;
        THub? hub = null;
        try
        {
            hubActivator = scope.ServiceProvider.GetRequiredService<IHubActivator<THub>>();
            hub = hubActivator.Create();

            if (!await IsHubMethodAuthorized(scope.ServiceProvider, connection, descriptor, hubMethodInvocationMessage.Arguments, hub))
            {
                Log.HubMethodNotAuthorized(_logger, hubMethodInvocationMessage.Target);
                await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                    $"Failed to invoke '{hubMethodInvocationMessage.Target}' because user is unauthorized");
                return true;
            }

            if (!await ValidateInvocationMode(descriptor, isStreamResponse, hubMethodInvocationMessage, connection))
            {
                return true;
            }

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
                    return true;
                }

                InitializeHub(hub, connection);
                Task? invocation = null;

                var arguments = hubMethodInvocationMessage.Arguments;
                CancellationTokenSource? cts = null;
                if (descriptor.HasSyntheticArguments)
                {
                    ReplaceArguments(descriptor, hubMethodInvocationMessage, isStreamCall, connection, scope, ref arguments, out cts);
                }

                if (isStreamCall || isStreamResponse)
                {
                    Debug.Assert(hub.Clients is HubCallerClients);
                    // Streaming invocations aren't involved with the semaphore.
                    // Setting the semaphore released flag avoids potential client result calls from the streaming hub method
                    // releasing the semaphore which would cause a SemaphoreFullException.
                    ((HubCallerClients)hub.Clients).TrySetSemaphoreReleased();
                }

                if (isStreamResponse)
                {
                    _ = StreamAsync(hubMethodInvocationMessage.InvocationId!, connection, arguments, scope, hubActivator, hub, cts, hubMethodInvocationMessage, descriptor);
                }
                else
                {
                    // Invoke or Send
                    static async Task ExecuteInvocation(DefaultHubDispatcher<THub> dispatcher,
                                                        ObjectMethodExecutor methodExecutor,
                                                        THub hub,
                                                        object?[] arguments,
                                                        AsyncServiceScope scope,
                                                        IHubActivator<THub> hubActivator,
                                                        HubConnectionContext connection,
                                                        HubMethodInvocationMessage hubMethodInvocationMessage,
                                                        bool isStreamCall)
                    {
                        var logger = dispatcher._logger;
                        var enableDetailedErrors = dispatcher._enableDetailedErrors;

                        // Hub invocation gets its parent from a remote source. Clear any current activity and restore it later.
                        var previousActivity = Activity.Current;
                        if (previousActivity != null)
                        {
                            Activity.Current = null;
                        }

                        // Use hubMethodInvocationMessage.Target instead of methodExecutor.MethodInfo.Name
                        // We want to take HubMethodNameAttribute into account which will be the same as what the invocation target is
                        var activity = StartActivity(SignalRServerActivitySource.InvocationIn, ActivityKind.Server, connection.OriginalActivity, scope.ServiceProvider, hubMethodInvocationMessage.Target, hubMethodInvocationMessage.Headers, logger);

                        object? result;
                        try
                        {
                            result = await dispatcher.ExecuteHubMethod(methodExecutor, hub, arguments, connection, scope.ServiceProvider);
                            Log.SendingResult(logger, hubMethodInvocationMessage.InvocationId, methodExecutor);
                        }
                        catch (Exception ex)
                        {
                            SetActivityError(activity, ex);

                            Log.FailedInvokingHubMethod(logger, hubMethodInvocationMessage.Target, ex);
                            await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                                ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, enableDetailedErrors));
                            return;
                        }
                        finally
                        {
                            activity?.Stop();

                            if (Activity.Current != previousActivity)
                            {
                                Activity.Current = previousActivity;
                            }

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
                            // Invoke Async, one response expected
                            await connection.WriteAsync(CompletionMessage.WithResult(hubMethodInvocationMessage.InvocationId, result));
                        }
                    }

                    invocation = ExecuteInvocation(this, methodExecutor, hub, arguments, scope, hubActivator, connection, hubMethodInvocationMessage, isStreamCall);
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
                    await invocation!;
                }
            }
            catch (TargetInvocationException ex)
            {
                Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                await SendInvocationError(hubMethodInvocationMessage.InvocationId, connection,
                    ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex.InnerException ?? ex, _enableDetailedErrors));
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
                if (hub?.Clients is HubCallerClients hubCallerClients)
                {
                    wasSemaphoreReleased = !hubCallerClients.TrySetSemaphoreReleased();
                }
                await CleanupInvocation(connection, hubMethodInvocationMessage, hubActivator, hub, scope);
            }
        }

        return !wasSemaphoreReleased;
    }

    private static ValueTask CleanupInvocation(HubConnectionContext connection, HubMethodInvocationMessage hubMessage, IHubActivator<THub>? hubActivator,
        THub? hub, AsyncServiceScope scope)
    {
        if (hubMessage.StreamIds != null)
        {
            foreach (var stream in hubMessage.StreamIds)
            {
                connection.StreamTracker.TryComplete(CompletionMessage.Empty(stream));
            }
        }

        if (hub != null)
        {
            hubActivator?.Release(hub);
        }

        return scope.DisposeAsync();
    }

    private async Task StreamAsync(string invocationId, HubConnectionContext connection, object?[] arguments, AsyncServiceScope scope,
        IHubActivator<THub> hubActivator, THub hub, CancellationTokenSource? streamCts, HubMethodInvocationMessage hubMethodInvocationMessage, HubMethodDescriptor descriptor)
    {
        string? error = null;

        streamCts ??= CancellationTokenSource.CreateLinkedTokenSource(connection.ConnectionAborted);

        // Hub invocation gets its parent from a remote source. Clear any current activity and restore it later.
        var previousActivity = Activity.Current;
        if (previousActivity != null)
        {
            Activity.Current = null;
        }

        var activity = StartActivity(SignalRServerActivitySource.InvocationIn, ActivityKind.Server, connection.OriginalActivity, scope.ServiceProvider, hubMethodInvocationMessage.Target, hubMethodInvocationMessage.Headers, _logger);

        try
        {
            if (!connection.ActiveRequestCancellationSources.TryAdd(invocationId, streamCts))
            {
                Log.InvocationIdInUse(_logger, invocationId);
                error = $"Invocation ID '{invocationId}' is already in use.";
                return;
            }

            object? result;
            try
            {
                result = await ExecuteHubMethod(descriptor.MethodExecutor, hub, arguments, connection, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                SetActivityError(activity, ex);

                Log.FailedInvokingHubMethod(_logger, hubMethodInvocationMessage.Target, ex);
                error = ErrorMessageHelper.BuildErrorMessage($"An unexpected error occurred invoking '{hubMethodInvocationMessage.Target}' on the server.", ex, _enableDetailedErrors);
                return;
            }

            if (result == null)
            {
                Log.InvalidReturnValueFromStreamingMethod(_logger, descriptor.MethodExecutor.MethodInfo.Name);
                error = $"The value returned by the streaming method '{descriptor.MethodExecutor.MethodInfo.Name}' is not a ChannelReader<> or IAsyncEnumerable<>.";
                return;
            }

            await using var enumerator = descriptor.FromReturnedStream(result, streamCts.Token);
            Log.StreamingResult(_logger, invocationId, descriptor.MethodExecutor);
            var streamItemMessage = new StreamItemMessage(invocationId, null);

            while (await enumerator.MoveNextAsync())
            {
                streamItemMessage.Item = enumerator.Current;
                // Send the stream item
                await connection.WriteAsync(streamItemMessage);
            }
        }
        catch (ChannelClosedException ex)
        {
            // If the channel closes from an exception in the streaming method, grab the innerException for the error from the streaming method
            var exception = ex.InnerException ?? ex;
            SetActivityError(activity, exception);

            Log.FailedStreaming(_logger, invocationId, descriptor.MethodExecutor.MethodInfo.Name, exception);
            error = ErrorMessageHelper.BuildErrorMessage("An error occurred on the server while streaming results.", exception, _enableDetailedErrors);
        }
        catch (Exception ex)
        {
            // If the streaming method was canceled we don't want to send a HubException message - this is not an error case
            if (!(ex is OperationCanceledException && streamCts.IsCancellationRequested))
            {
                SetActivityError(activity, ex);

                Log.FailedStreaming(_logger, invocationId, descriptor.MethodExecutor.MethodInfo.Name, ex);
                error = ErrorMessageHelper.BuildErrorMessage("An error occurred on the server while streaming results.", ex, _enableDetailedErrors);
            }
        }
        finally
        {
            activity?.Stop();

            if (Activity.Current != previousActivity)
            {
                Activity.Current = previousActivity;
            }

            await CleanupInvocation(connection, hubMethodInvocationMessage, hubActivator, hub, scope);

            streamCts.Dispose();
            connection.ActiveRequestCancellationSources.TryRemove(invocationId, out _);

            await connection.WriteAsync(CompletionMessage.WithError(invocationId, error));
        }
    }

    private ValueTask<object?> ExecuteHubMethod(ObjectMethodExecutor methodExecutor, THub hub, object?[] arguments, HubConnectionContext connection, IServiceProvider serviceProvider)
    {
        if (_invokeMiddleware != null)
        {
            var invocationContext = new HubInvocationContext(methodExecutor, connection.HubCallerContext, serviceProvider, hub, arguments);
            return _invokeMiddleware(invocationContext);
        }

        // If no Hub filters are registered
        return ExecuteMethod(methodExecutor, hub, arguments);
    }

    private ValueTask<object?> ExecuteMethod(string hubMethodName, Hub hub, object?[] arguments)
    {
        if (!_methods.TryGetValue(hubMethodName, out var methodDescriptor))
        {
            throw new HubException($"Unknown hub method '{hubMethodName}'");
        }
        var methodExecutor = methodDescriptor.MethodExecutor;
        return ExecuteMethod(methodExecutor, hub, arguments);
    }

    private static async ValueTask<object?> ExecuteMethod(ObjectMethodExecutor methodExecutor, Hub hub, object?[] arguments)
    {
        if (methodExecutor.IsMethodAsync)
        {
            if (methodExecutor.MethodReturnType == typeof(Task))
            {
                await (Task)methodExecutor.Execute(hub, arguments)!;
                return null;
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
    }

    private static async Task SendInvocationError(string? invocationId, HubConnectionContext connection, string errorMessage)
    {
        if (string.IsNullOrEmpty(invocationId))
        {
            return;
        }

        await connection.WriteAsync(CompletionMessage.WithError(invocationId, errorMessage));
    }

    private void InitializeHub(THub hub, HubConnectionContext connection, bool invokeAllowed = true)
    {
        hub.Clients = new HubCallerClients(_hubContext.Clients, connection.ConnectionId, connection.ActiveInvocationLimit) { InvokeAllowed = invokeAllowed };
        hub.Context = connection.HubCallerContext;
        hub.Groups = _hubContext.Groups;
    }

    private static Task<bool> IsHubMethodAuthorized(IServiceProvider provider, HubConnectionContext hubConnectionContext, HubMethodDescriptor descriptor, object?[] hubMethodArguments, Hub hub)
    {
        // If there are no policies we don't need to run auth
        if (descriptor.Policies.Count == 0)
        {
            return TaskCache.True;
        }

        return IsHubMethodAuthorizedSlow(provider, hubConnectionContext.User, descriptor.Policies, new HubInvocationContext(hubConnectionContext.HubCallerContext, provider, hub, descriptor.MethodExecutor.MethodInfo, hubMethodArguments));
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
            await connection.WriteAsync(CompletionMessage.WithError(hubMethodInvocationMessage.InvocationId!,
                $"The client attempted to invoke the non-streaming '{hubMethodInvocationMessage.Target}' method with a streaming invocation."));

            return false;
        }

        return true;
    }

    private void ReplaceArguments(HubMethodDescriptor descriptor, HubMethodInvocationMessage hubMethodInvocationMessage, bool isStreamCall,
        HubConnectionContext connection, AsyncServiceScope scope, ref object?[] arguments, out CancellationTokenSource? cts)
    {
        cts = null;
        // In order to add the synthetic arguments we need a new array because the invocation array is too small (it doesn't know about synthetic arguments)
        arguments = new object?[descriptor.OriginalParameterTypes!.Count];

        var streamPointer = 0;
        var hubInvocationArgumentPointer = 0;
        for (var parameterPointer = 0; parameterPointer < arguments.Length; parameterPointer++)
        {
            if (hubMethodInvocationMessage.Arguments?.Length > hubInvocationArgumentPointer &&
                (hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer] == null ||
                descriptor.OriginalParameterTypes[parameterPointer].IsAssignableFrom(hubMethodInvocationMessage.Arguments[hubInvocationArgumentPointer]?.GetType())))
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
                else if (descriptor.IsServiceArgument(parameterPointer))
                {
                    arguments[parameterPointer] = descriptor.GetService(scope.ServiceProvider, parameterPointer, descriptor.OriginalParameterTypes[parameterPointer]);
                }
                else if (isStreamCall && ReflectionHelper.IsStreamingType(descriptor.OriginalParameterTypes[parameterPointer], mustBeDirectType: true))
                {
                    Log.StartingParameterStream(_logger, hubMethodInvocationMessage.StreamIds![streamPointer]);
                    var itemType = descriptor.StreamingParameters![streamPointer];
                    arguments[parameterPointer] = connection.StreamTracker.AddStream(hubMethodInvocationMessage.StreamIds[streamPointer],
                        itemType, descriptor.OriginalParameterTypes[parameterPointer]);

                    streamPointer++;
                }
                else
                {
                    // This should never happen
                    Debug.Assert(false, $"Failed to bind argument of type '{descriptor.OriginalParameterTypes[parameterPointer].Name}' for hub method '{descriptor.MethodExecutor.MethodInfo.Name}'.");
                }
            }
        }
    }

    private void DiscoverHubMethods(bool disableImplicitFromServiceParameters)
    {
        var hubType = typeof(THub);
        var hubTypeInfo = hubType.GetTypeInfo();
        var hubName = hubType.Name;

        using var scope = _serviceScopeFactory.CreateScope();

        IServiceProviderIsService? serviceProviderIsService = null;
        if (!disableImplicitFromServiceParameters)
        {
            serviceProviderIsService = scope.ServiceProvider.GetService<IServiceProviderIsService>();
        }

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

            var executor = IsCustomAwaitableSupported
                ? CreateObjectMethodExecutor(methodInfo, hubTypeInfo)
                : ObjectMethodExecutor.CreateTrimAotCompatible(methodInfo, hubTypeInfo);

            var authorizeAttributes = methodInfo.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
            _methods[methodName] = new HubMethodDescriptor(executor, serviceProviderIsService, authorizeAttributes);
            _cachedMethodNames.Add(methodName);

            Log.HubMethodBound(_logger, hubName, methodName);
        }
    }

    [RequiresUnreferencedCode("Using SignalR with 'Microsoft.AspNetCore.SignalR.Hub.IsCustomAwaitableSupported=true' is not trim compatible.")]
    [RequiresDynamicCode("Using SignalR with 'Microsoft.AspNetCore.SignalR.Hub.IsCustomAwaitableSupported=true' is not native AOT compatible.")]
    private static ObjectMethodExecutor CreateObjectMethodExecutor(MethodInfo methodInfo, TypeInfo targetType)
       => ObjectMethodExecutor.Create(methodInfo, targetType);

    public override IReadOnlyList<Type> GetParameterTypes(string methodName)
    {
        if (!_methods.TryGetValue(methodName, out var descriptor))
        {
            throw new HubException("Method does not exist.");
        }
        return descriptor.ParameterTypes;
    }

    public override string? GetTargetName(ReadOnlySpan<byte> targetUtf8Bytes)
    {
        if (_cachedMethodNames.TryGetValue(targetUtf8Bytes, out var targetName))
        {
            return targetName;
        }

        return null;
    }

    // Starts an Activity for a Hub method invocation and sets up all the tags and other state.
    // Make sure to call Activity.Stop() once the Hub method completes, and consider calling SetActivityError on exception.
    private static Activity? StartActivity(string operationName, ActivityKind kind, Activity? linkedActivity, IServiceProvider serviceProvider, string methodName, IDictionary<string, string>? headers, ILogger logger)
    {
        var activitySource = serviceProvider.GetService<SignalRServerActivitySource>()?.ActivitySource;
        if (activitySource is null)
        {
            return null;
        }

        var loggingEnabled = logger.IsEnabled(LogLevel.Critical);
        if (!activitySource.HasListeners() && !loggingEnabled)
        {
            return null;
        }

        IEnumerable<KeyValuePair<string, object?>> tags =
        [
            new("rpc.method", methodName),
            new("rpc.system", "signalr"),
            new("rpc.service", _fullHubName),
            // See https://github.com/dotnet/aspnetcore/blob/027c60168383421750f01e427e4f749d0684bc02/src/Servers/Kestrel/Core/src/Internal/Infrastructure/KestrelMetrics.cs#L308
            // And https://github.com/dotnet/aspnetcore/issues/43786
            //new("server.address", ...),
        ];
        IEnumerable<ActivityLink>? links = (linkedActivity is not null) ? [new ActivityLink(linkedActivity.Context)] : null;

        Activity? activity;
        if (headers != null)
        {
            var propagator = serviceProvider.GetService<DistributedContextPropagator>() ?? DistributedContextPropagator.Current;

            activity = ActivityCreator.CreateFromRemote(
                activitySource,
                propagator,
                headers,
                static (object? carrier, string fieldName, out string? fieldValue, out IEnumerable<string>? fieldValues) =>
                {
                    fieldValues = default;
                    var headers = (IDictionary<string, string>)carrier!;
                    headers.TryGetValue(fieldName, out fieldValue);
                },
                operationName,
                kind,
                tags,
                links,
                loggingEnabled);
        }
        else
        {
            activity = activitySource.CreateActivity(operationName, kind, parentId: null, tags: tags, links: links);
        }

        if (activity is not null)
        {
            activity.DisplayName = $"{_fullHubName}/{methodName}";
            activity.Start();
        }

        return activity;
    }

    private static void SetActivityError(Activity? activity, Exception ex)
    {
        activity?.SetTag("error.type", ex.GetType().FullName);
        activity?.SetStatus(ActivityStatusCode.Error);
    }
}
