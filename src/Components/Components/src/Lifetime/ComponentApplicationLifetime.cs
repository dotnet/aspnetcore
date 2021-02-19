// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Components.Lifetime
{
    /// <summary>
    /// Manages the lifetime of a component application.
    /// </summary>
    public class ComponentApplicationLifetime
    {
        private bool _stateIsPersisted;
        private List<ComponentApplicationState.OnPersistingCallback> _pauseCallbacks = new();
        private readonly Dictionary<string, byte[]> _currentState = new();
        private readonly ILogger<ComponentApplicationLifetime> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ComponentApplicationLifetime"/>.
        /// </summary>
        public ComponentApplicationLifetime(ILogger<ComponentApplicationLifetime> logger)
        {
            State = new ComponentApplicationState(_currentState, _pauseCallbacks);
            _logger = logger;
        }

        /// <summary>
        /// Gets the <see cref="ComponentApplicationState"/> associated with the <see cref="ComponentApplicationLifetime"/>.
        /// </summary>
        public ComponentApplicationState State { get; }

        /// <summary>
        /// Restores the component application state from the given <see cref="IComponentApplicationStateStore"/>.
        /// </summary>
        /// <param name="store">The <see cref="IComponentApplicationStateStore"/> to restore the application state from.</param>
        /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
        public async Task RestoreStateAsync(IComponentApplicationStateStore store)
        {
            var data = await store.GetPersistedStateAsync();
            State.InitializeExistingState(data);
        }

        /// <summary>
        /// Persists the component application state into the given <see cref="IComponentApplicationStateStore"/>.
        /// </summary>
        /// <param name="store">The <see cref="IComponentApplicationStateStore"/> to restore the application state from.</param>
        /// <param name="renderer">The <see cref="Renderer"/> that components are being rendered.</param>
        /// <returns>A <see cref="Task"/> that will complete when the state has been restored.</returns>
        public Task PersistStateAsync(IComponentApplicationStateStore store, Renderer renderer)
        {
            if (_stateIsPersisted)
            {
                throw new InvalidOperationException("State already persisted.");
            }

            _stateIsPersisted = true;

            return renderer.Dispatcher.InvokeAsync(PauseAndPersistState);

            async Task PauseAndPersistState()
            {
                await PauseAsync();

                var data = new ReadOnlyDictionary<string, byte[]>(_currentState);
                await store.PersistStateAsync(data);
            }
        }

        internal Task PauseAsync()
        {
            List<Task>? pendingCallbackTasks = null;

            for (int i = 0; i < _pauseCallbacks.Count; i++)
            {
                var callback = _pauseCallbacks[i];
                var result = ExecuteCallback(callback, _logger);
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

            static Task ExecuteCallback(ComponentApplicationState.OnPersistingCallback callback, ILogger<ComponentApplicationLifetime> logger)
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

                static async Task Awaited(Task task, ILogger<ComponentApplicationLifetime> logger)
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
}
