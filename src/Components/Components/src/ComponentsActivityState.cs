// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components;

internal sealed partial class ComponentsActivityState : IDisposable
{
    private const string PersistenceKey = "Microsoft.AspNetCore.Components.ActivityState";
    private readonly PersistentComponentState _state;
    private readonly ComponentsActivityLinkStore _activityLinkStore;
    private readonly ComponentsActivitySource _activitySource;
    private readonly ILogger _logger;
    private readonly PersistingComponentStateSubscription _persistingSubscription;
    private readonly RestoringComponentStateSubscription _restoringSubscription;
    private RouteState? _pendingState;
    private bool _hasPendingState;

    public ComponentsActivityState(
        ComponentStatePersistenceManager manager,
        IComponentRenderMode renderMode,
        ComponentsActivityLinkStore activityLinkStore,
        ComponentsActivitySource activitySource,
        ILogger logger)
    {
        _state = manager.State;
        _activityLinkStore = activityLinkStore;
        _activitySource = activitySource;
        _logger = logger;
        _activitySource.LastRouterUnregistered += ApplyPendingState;
        _persistingSubscription = _state.RegisterOnPersisting(PersistAsync, renderMode);
        _restoringSubscription = _state.RegisterOnRestoring(Restore, new RestoreOptions { AllowUpdates = true });
    }

    private Task PersistAsync()
    {
        RouteState? routeState = null;
        if (ComponentsActivitySource.HasListeners &&
            _activityLinkStore.TryGetActivityContext(ComponentsActivityLinkStore.Route, out var context, out var tag) &&
            context != default &&
            tag is { Value: string route })
        {
            routeState = new(
                $"00-{context.TraceId}-{context.SpanId}-{(byte)context.TraceFlags:x2}",
                context.TraceState,
                context.IsRemote,
                route);
        }

        // Retained interactive roots need a clear update even when the new route is not traced.
        _state.PersistAsBytes(PersistenceKey, JsonSerializer.SerializeToUtf8Bytes(routeState, RouteStateJsonContext.Default.RouteState));
        return Task.CompletedTask;
    }

    private void Restore()
    {
        _pendingState = _state.TryTakeBytes(PersistenceKey, out var data)
            ? JsonSerializer.Deserialize(data, RouteStateJsonContext.Default.RouteState)
            : null;
        _hasPendingState = true;
        ApplyPendingState();
    }

    private void ApplyPendingState()
    {
        // An interactive router owns correlation until its last instance is removed.
        if (!_hasPendingState || _activitySource.HasActiveRouter)
        {
            return;
        }

        _activityLinkStore.RemoveActivityContext(ComponentsActivityLinkStore.Route);
        if (_pendingState is { } routeState)
        {
            if (ActivityContext.TryParse(routeState.TraceParent, routeState.TraceState,
                isRemote: OperatingSystem.IsBrowser() || routeState.IsRemote, out var context))
            {
                _activityLinkStore.SetActivityContext(
                    ComponentsActivityLinkStore.Route,
                    context,
                    new KeyValuePair<string, object?>("aspnetcore.components.route", routeState.Route));
            }
            else
            {
                _logger.LogDebug("The persisted navigation activity context could not be restored.");
            }
        }

        _pendingState = null;
        _hasPendingState = false;
    }

    public void Dispose()
    {
        _persistingSubscription.Dispose();
        _restoringSubscription.Dispose();
        _activitySource.LastRouterUnregistered -= ApplyPendingState;
    }

    internal sealed record RouteState(string TraceParent, string? TraceState, bool IsRemote, string Route);

    [JsonSerializable(typeof(RouteState))]
    private sealed partial class RouteStateJsonContext : JsonSerializerContext;
}
