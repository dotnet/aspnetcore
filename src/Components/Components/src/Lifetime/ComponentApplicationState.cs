// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// The state for the components and services of a components application.
    /// </summary>
    public class ComponentApplicationState
    {
        private IDictionary<string, byte[]>? _existingState;
        private readonly IDictionary<string, byte[]> _currentState;
        private readonly List<OnPersistingCallback> _registeredCallbacks;

        internal ComponentApplicationState(
            IDictionary<string, byte[]> currentState,
            List<OnPersistingCallback> pauseCallbacks)
        {
            _currentState = currentState;
            _registeredCallbacks = pauseCallbacks;
        }

        internal void InitializeExistingState(IDictionary<string, byte[]> existingState)
        {
            if (_existingState != null)
            {
                throw new InvalidOperationException("ComponentApplicationState already initialized.");
            }
            _existingState = existingState ?? throw new ArgumentNullException(nameof(existingState));
        }

        /// <summary>
        /// Represents the method that performs operations when <see cref="OnPersisting"/> is raised and the application is about to be paused.
        /// </summary>
        /// <returns>A <see cref="Task"/> that will complete when the method is done preparing for the application pause.</returns>
        public delegate Task OnPersistingCallback();

        /// <summary>
        /// An event that is raised when the application is about to be paused.
        /// Registered handlers can use this opportunity to persist their state so that it can be retrieved when the application resumes.
        /// </summary>
        public event OnPersistingCallback OnPersisting
        {
            add
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _registeredCallbacks.Add(value);
            }
            remove
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _registeredCallbacks.Remove(value);
            }
        }

        /// <summary>
        /// Tries to retrieve the persisted state with the given <paramref name="key"/>.
        /// When the key is present, the state is successfully returned via <paramref name="value"/>
        /// and removed from the <see cref="ComponentApplicationState"/>.
        /// </summary>
        /// <param name="key">The key used to persist the state.</param>
        /// <param name="value">The persisted state.</param>
        /// <returns><c>true</c> if the state was found; <c>false</c> otherwise.</returns>
        public bool TryRedeemPersistedState(string key, [MaybeNullWhen(false)] out byte[]? value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_existingState == null)
            {
                // Services during prerendering might try to access their state upon injection on the page
                // and we don't want to fail in that case.
                // When a service is prerendering there is no state to restore and in other cases the host
                // is responsible for initializing the state before services or components can access it.
                value = null;
                return false;
            }

            if (_existingState.TryGetValue(key, out value))
            {
                _existingState.Remove(key);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Persists the serialized state <paramref name="value"/> for the given <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to use to persist the state.</param>
        /// <param name="value">The state to persist.</param>
        public void PersistState(string key, byte[] value)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (_currentState.ContainsKey(key))
            {
                throw new ArgumentException($"There is already a persisted object under the same key '{key}'");
            }
            _currentState.Add(key, value);
        }

        /// <summary>
        /// Serializes <paramref name="instance"/> as JSON and persists it under the given <paramref name="key"/>.
        /// </summary>
        /// <typeparam name="TValue">The <paramref name="instance"/> type.</typeparam>
        /// <param name="key">The key to use to persist the state.</param>
        /// <param name="instance">The instance to persist.</param>
        public void PersistAsJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, TValue instance)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            PersistState(key, JsonSerializer.SerializeToUtf8Bytes(instance, JsonSerializerOptionsProvider.Options));
        }

        /// <summary>
        /// Tries to retrieve the persisted state as JSON with the given <paramref name="key"/> and deserializes it into an
        /// instance of type <typeparamref name="TValue"/>.
        /// When the key is present, the state is successfully returned via <paramref name="instance"/>
        /// and removed from the <see cref="ComponentApplicationState"/>.
        /// </summary>
        /// <param name="key">The key used to persist the instance.</param>
        /// <param name="instance">The persisted instance.</param>
        /// <returns><c>true</c> if the state was found; <c>false</c> otherwise.</returns>
        public bool TryRedeemFromJson<[DynamicallyAccessedMembers(JsonSerialized)] TValue>(string key, [MaybeNullWhen(false)] out TValue? instance)
        {
            if (key is null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (TryRedeemPersistedState(key, out var data))
            {
                instance = JsonSerializer.Deserialize<TValue>(data)!;
                return true;
            }
            else
            {
                instance = default(TValue);
                return false;
            }
        }
    }
}
