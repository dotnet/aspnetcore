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
    private readonly List<PersistComponentStateRegistration> _registeredCallbacks = new();
    private readonly ILogger<ComponentStatePersistenceManager> _logger;

    private bool _stateIsPersisted;
    private readonly Dictionary<string, byte[]> _currentState = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of <see cref="ComponentStatePersistenceManager"/>.
    /// </summary>
    public ComponentStatePersistenceManager(ILogger<ComponentStatePersistenceManager> logger)
    {
        State = new PersistentComponentState(_currentState, _registeredCallbacks);
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
    /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
    public async Task RestoreStateAsync(IPersistentComponentStateStore store)
    {
        var data = await store.GetPersistedStateAsync();
        State.InitializeExistingState(data);
    }

    /// <summary>
    /// Persists the component application state into the given <see cref="IPersistentComponentStateStore"/>.
    /// </summary>
    /// <param name="store">The <see cref="IPersistentComponentStateStore"/> to restore the application state from.</param>
    /// <param name="renderer">The <see cref="Renderer"/> that components are being rendered.</param>
    /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
    public Task PersistStateAsync(IPersistentComponentStateStore store, Renderer renderer)
    {
        if (_stateIsPersisted)
        {
            throw new InvalidOperationException("State already persisted.");
        }

        return renderer.Dispatcher.InvokeAsync(PauseAndPersistState);

        async Task PauseAndPersistState()
        {
            State.PersistingState = true;

            if (store is IEnumerable<IPersistentComponentStateStore> compositeStore)
            {
                // We only need to do inference when there is more than one store. This is determined by
                // the set of rendered components.
                InferRenderModes(renderer);

                // Iterate over each store and give it a chance to run against the existing declared
                // render modes. After we've run through a store, we clear the current state so that
                // the next store can start with a clean slate.
                foreach (var store in compositeStore)
                {
                    await PersistState(store);
                    _currentState.Clear();
                }
            }
            else
            {
                await PersistState(store);
            }

            State.PersistingState = false;
            _stateIsPersisted = true;
        }

        async Task PersistState(IPersistentComponentStateStore store)
        {
            await PauseAsync(store);
            await store.PersistStateAsync(_currentState);
        }
    }

    private void InferRenderModes(Renderer renderer)
    {
        for (var i = 0; i < _registeredCallbacks.Count; i++)
        {
            var registration = _registeredCallbacks[i];
            if (registration.RenderMode != null)
            {
                // Explicitly set render mode, so nothing to do.
                continue;
            }

            if (registration.Callback.Target is IComponent component)
            {
                var componentRenderMode = renderer.GetComponentRenderMode(component);
                if (componentRenderMode != null)
                {
                    _registeredCallbacks[i] = new PersistComponentStateRegistration(registration.Callback, componentRenderMode);
                }
                else
                {
                    // If we can't find a render mode, it's an SSR only component and we don't need to
                    // persist its state at all.
                    _registeredCallbacks[i] = default;
                }
                continue;
            }

            throw new InvalidOperationException(
                $"The registered callback {registration.Callback.Method.Name} must be associated with a component or define" +
                $" an explicit render mode type during registration.");
        }
    }

    internal Task PauseAsync(IPersistentComponentStateStore store)
    {
        List<Task>? pendingCallbackTasks = null;

        for (var i = 0; i < _registeredCallbacks.Count; i++)
        {
            var registration = _registeredCallbacks[i];

            if (!store.SupportsRenderMode(registration.RenderMode!))
            {
                // The callback does not have an associated render mode and we are in a multi-store scenario.
                // Otherwise, in a single store scenario, we just run the callback.
                // If the registration callback is null, it's because it was associated with a component and we couldn't infer
                // its render mode, which means is an SSR only component and we don't need to persist its state at all.
                continue;
            }

            var result = ExecuteCallback(registration.Callback, _logger);
            if (!result.IsCompletedSuccessfully)
            {
                pendingCallbackTasks ??= new();
                pendingCallbackTasks.Add(result);
            }
        }

        if (pendingCallbackTasks != null)
        {
            return Task.WhenAll(pendingCallbackTasks);
        }
        else
        {
            return Task.CompletedTask;
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
