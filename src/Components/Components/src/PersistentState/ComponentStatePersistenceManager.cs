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
    private readonly PersistentServicesRegistry? _servicesRegistry;
    private readonly Dictionary<string, byte[]> _currentState = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of <see cref="ComponentStatePersistenceManager"/>.
    /// </summary>
    /// <param name="logger"></param>
    public ComponentStatePersistenceManager(ILogger<ComponentStatePersistenceManager> logger)
    {
        State = new PersistentComponentState(_currentState, _registeredCallbacks);
        _logger = logger;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ComponentStatePersistenceManager"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="serviceProvider"></param>
    public ComponentStatePersistenceManager(ILogger<ComponentStatePersistenceManager> logger, IServiceProvider serviceProvider) : this(logger)
    {
        _servicesRegistry = new PersistentServicesRegistry(serviceProvider);
    }

    // For testing purposes only
    internal PersistentServicesRegistry? ServicesRegistry => _servicesRegistry;

    // For testing purposes only
    internal List<PersistComponentStateRegistration> RegisteredCallbacks => _registeredCallbacks;

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
        _servicesRegistry?.Restore(State);
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
            // Ensure that we register the services before we start persisting the state.
            _servicesRegistry?.RegisterForPersistence(State);

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
                    var result = await TryPersistState(store);
                    if (!result)
                    {
                        break;
                    }
                    _currentState.Clear();
                }
            }
            else
            {
                await TryPersistState(store);
            }

            State.PersistingState = false;
            _stateIsPersisted = true;
        }

        async Task<bool> TryPersistState(IPersistentComponentStateStore store)
        {
            if (!await TryPauseAsync(store))
            {
                _currentState.Clear();
                return false;
            }

            await store.PersistStateAsync(_currentState);
            return true;
        }
    }

    /// <summary>
    /// Initializes the render mode for state persisted by the platform.
    /// </summary>
    /// <param name="renderMode">The render mode to use for state persisted by the platform.</param>
    /// <exception cref="InvalidOperationException">when the render mode is already set.</exception>
    public void SetPlatformRenderMode(IComponentRenderMode renderMode)
    {
        if (_servicesRegistry == null)
        {
            return;
        }
        else if (_servicesRegistry?.RenderMode != null)
        {
            throw new InvalidOperationException("Render mode already set.");
        }

        _servicesRegistry!.RenderMode = renderMode;
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

    internal Task<bool> TryPauseAsync(IPersistentComponentStateStore store)
    {
        List<Task<bool>>? pendingCallbackTasks = null;

        // We are iterating backwards to allow the callbacks to remove themselves from the list.
        // Otherwise, we would have to make a copy of the list to avoid running into situations
        // where we don't run all the callbacks because the count of the list changed while we
        // were iterating over it.
        // It is not allowed to register a callback while we are persisting the state, so we don't
        // need to worry about new callbacks being added to the list.
        for (var i = _registeredCallbacks.Count - 1; i >= 0; i--)
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

            var result = TryExecuteCallback(registration.Callback, _logger);
            if (!result.IsCompletedSuccessfully)
            {
                pendingCallbackTasks ??= [];
                pendingCallbackTasks.Add(result);
            }
            else
            {
                if (!result.Result)
                {
                    return Task.FromResult(false);
                }
            }
        }

        if (pendingCallbackTasks != null)
        {
            return AnyTaskFailed(pendingCallbackTasks);
        }
        else
        {
            return Task.FromResult(true);
        }

        static Task<bool> TryExecuteCallback(Func<Task> callback, ILogger<ComponentStatePersistenceManager> logger)
        {
            try
            {
                var current = callback();
                if (current.IsCompletedSuccessfully)
                {
                    return Task.FromResult(true);
                }
                else
                {
                    return Awaited(current, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(new EventId(1000, "PersistenceCallbackError"), ex, "There was an error executing a callback while pausing the application.");
                return Task.FromResult(false);
            }

            static async Task<bool> Awaited(Task task, ILogger<ComponentStatePersistenceManager> logger)
            {
                try
                {
                    await task;
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(new EventId(1000, "PersistenceCallbackError"), ex, "There was an error executing a callback while pausing the application.");
                    return false;
                }
            }
        }

        static async Task<bool> AnyTaskFailed(List<Task<bool>> pendingCallbackTasks)
        {
            foreach (var result in await Task.WhenAll(pendingCallbackTasks))
            {
                if (!result)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
