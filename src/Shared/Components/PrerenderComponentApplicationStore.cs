// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Components
{
    internal class PrerenderComponentApplicationStore : IComponentApplicationStateStore
    {
        public PrerenderComponentApplicationStore()
        {
            ExistingState = new();
        }

        public PrerenderComponentApplicationStore(string existingState)
        {
            if (existingState is null)
            {
                throw new ArgumentNullException(nameof(existingState));
            }

            ExistingState = JsonSerializer.Deserialize<Dictionary<string, byte[]>>(Convert.FromBase64String(existingState)) ??
                throw new ArgumentException(nameof(existingState));
        }

#nullable enable
        public string? PersistedState { get; private set; }
#nullable disable

        public Dictionary<string, byte[]> ExistingState { get; init; }

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
        {
            return ExistingState != null ?
                Task.FromResult((IDictionary<string, byte[]>)ExistingState) :
                throw new InvalidOperationException("The store was not initialized with any state.");
        }

        protected virtual byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state)
        {
            return JsonSerializer.SerializeToUtf8Bytes(state);
        }

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            var bytes = SerializeState(state);

            var result = Convert.ToBase64String(bytes);
            PersistedState = result;
            return Task.CompletedTask;
        }
    }
}
