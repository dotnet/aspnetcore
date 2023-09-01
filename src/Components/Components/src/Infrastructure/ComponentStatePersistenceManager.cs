// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Infrastructure;

/// <summary>
/// Manages the persistent state of components in an application.
/// </summary>
public class ComponentStatePersistenceManager
{
    private bool _serverStateIsPersisted;
    private bool _webAssemblyStateIsPersisted;

    private readonly List<Func<Task>> _serverCallbacks = new();
    private readonly List<Func<Task>> _webAssemblyCallbacks = new();

    private readonly Dictionary<string, byte[]> _currentServerState = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]> _currentWebAssemblyState = new(StringComparer.Ordinal);

    private readonly ILogger<ComponentStatePersistenceManager> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ComponentStatePersistenceManager"/>.
    /// </summary>
    public ComponentStatePersistenceManager(ILogger<ComponentStatePersistenceManager> logger, IComponentSerializationModeHandler serializationModeHandler)
    {
        State = new PersistentComponentState(
            _currentServerState,
            _currentWebAssemblyState,
            _serverCallbacks,
            _webAssemblyCallbacks,
            serializationModeHandler);

        _logger = logger;
    }

    /// <summary>
    /// Gets the <see cref="ComponentStatePersistenceManager"/> associated with the <see cref="ComponentStatePersistenceManager"/>.
    /// </summary>
    public PersistentComponentState State { get; }

    /// <summary>
    /// Restores the component application state from the given <see cref="IPersistentComponentStateStore"/>.
    /// </summary>
    /// <param name="store">The <see cref="IPersistentComponentStateStore"/> to restore the application state from.</param>
    /// <param name="serializationMode">The <see cref="PersistedStateSerializationMode"/> to restore the application state.</param>
    /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
    public async Task RestoreStateAsync(IPersistentComponentStateStore store, PersistedStateSerializationMode serializationMode)
    {
        var data = await store.GetPersistedStateAsync();
        State.InitializeExistingState(data);
        // We need to set the serialization in order to register callbacks later
        State.SerializationMode = serializationMode;
    }

    /// <summary>
    /// Persists the component application state into the given <see cref="IPersistentComponentStateStore"/>.
    /// </summary>
    /// <param name="store">The <see cref="IPersistentComponentStateStore"/> to persist the application state into.</param>
    /// <param name="serializationMode">The <see cref="PersistedStateSerializationMode"/> to persist the application state.</param>
    /// <param name="renderer">The <see cref="Renderer"/> that components are being rendered.</param>
    /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
    public Task PersistStateAsync(
        IPersistentComponentStateStore store,
        PersistedStateSerializationMode serializationMode,
        Renderer renderer)
        => PersistStateAsync(store, serializationMode, renderer.Dispatcher);

    /// <summary>
    /// Persists the component application state into the given <see cref="IPersistentComponentStateStore"/>
    /// so that it could be restored on Server.
    /// </summary>
    /// <param name="store">The <see cref="IPersistentComponentStateStore"/> to persist the application state into.</param>
    /// <param name="serializationMode">The <see cref="PersistedStateSerializationMode"/> to persist the application state.</param>
    /// <param name="dispatcher">The <see cref="Dispatcher"/> corresponding to the components' renderer.</param>
    /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
    public Task PersistStateAsync(
        IPersistentComponentStateStore store,
        PersistedStateSerializationMode serializationMode,
        Dispatcher dispatcher)
    {
        switch (serializationMode)
        {
            case PersistedStateSerializationMode.Server:
                if (_serverStateIsPersisted)
                {
                    throw new InvalidOperationException("State already persisted.");
                }
                _serverStateIsPersisted = true;
                return PersistStateAsync(store, serializationMode, _serverCallbacks, _currentServerState, dispatcher);

            case PersistedStateSerializationMode.WebAssembly:
                if (_webAssemblyStateIsPersisted)
                {
                    throw new InvalidOperationException("State already persisted.");
                }
                _webAssemblyStateIsPersisted = true;
                return PersistStateAsync(store, serializationMode, _webAssemblyCallbacks, _currentWebAssemblyState, dispatcher);

            default:
                throw new InvalidOperationException("Invalid persistence mode");
        }
    }

    private Task PersistStateAsync(
        IPersistentComponentStateStore store,
        PersistedStateSerializationMode serializationMode,
        List<Func<Task>> callbacks,
        Dictionary<string, byte[]> currentState,
        Dispatcher dispatcher)
    {
        return dispatcher.InvokeAsync(PauseAndPersistState);

        async Task PauseAndPersistState()
        {
            State.PersistingState = true;
            State.SerializationMode = serializationMode;

            await PauseAsync(callbacks);

            State.PersistingState = false;

            await store.PersistStateAsync(currentState);
        }
    }

    internal async Task PauseAsync(List<Func<Task>> callbacks)
    {
        List<Task>? pendingCallbackTasks = null;

        for (var i = 0; i < callbacks.Count; i++)
        {
            var callback = callbacks[i];
            var result = ExecuteCallback(callback, _logger);
            if (!result.IsCompletedSuccessfully)
            {
                pendingCallbackTasks ??= new();
                pendingCallbackTasks.Add(result);
            }
        }

        if (pendingCallbackTasks != null)
        {
            await Task.WhenAll(pendingCallbackTasks);
        }

        static Task ExecuteCallback(Func<Task> callback, ILogger<ComponentStatePersistenceManager> logger)
        {
            try
            {
                var current = callback();
                if (current.IsCompletedSuccessfully)
                {
                    return current;
                }
                else
                {
                    return Awaited(current, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(1000, "PersistenceCallbackError"), ex, "There was an error executing a callback while pausing the application.");
                return Task.CompletedTask;
            }

            static async Task Awaited(Task task, ILogger<ComponentStatePersistenceManager> logger)
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(1000, "PersistenceCallbackError"), ex, "There was an error executing a callback while pausing the application.");
                    return;
                }
            }
        }
    }
}
