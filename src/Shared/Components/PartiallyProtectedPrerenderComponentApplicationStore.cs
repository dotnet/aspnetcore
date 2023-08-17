// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;

namespace Microsoft.AspNetCore.Components;

internal sealed class PartiallyProtectedPrerenderComponentApplicationStore : IPersistentComponentStateStore
#pragma warning restore CA1852 // Seal internal types
{
    private readonly IDataProtector _protector = default!; // Assigned in all constructor paths

    public PartiallyProtectedPrerenderComponentApplicationStore(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("Microsoft.AspNetCore.Components.Server.State");
    }

#nullable enable
    public string? PersistedState { get; private set; }

    public string? PersistedProtectedState { get; private set; }
#nullable disable

    public Task<IDictionary<string, byte[]>> GetPersistedStateAsync()
    {
        // GetPersistedStateAsync is never called.
        throw new NotImplementedException();
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Simple serialize of primitive types.")]
    private static byte[] SerializeState(IReadOnlyDictionary<string, byte[]> state) =>
        JsonSerializer.SerializeToUtf8Bytes(state);

    private byte[] SerializeAndProtectState(IReadOnlyDictionary<string, byte[]> state)
    {
        var bytes = SerializeState(state);
        return _protector != null ? _protector.Protect(bytes) : bytes;
    }

    public Task PersistStateAsync(IReadOnlyDictionary<string, Tuple<PersistComponentStateDirection, byte[]>> state)
    {
        Dictionary<string, byte[]> serverState = new(StringComparer.Ordinal);
        Dictionary<string, byte[]> webAssemblyState = new(StringComparer.Ordinal);

        foreach (var entry in state)
        {
            var direction = entry.Value.Item1;

            switch (direction)
            {
                case PersistComponentStateDirection.Server:
                    serverState[entry.Key] = entry.Value.Item2;
                    break;
                case PersistComponentStateDirection.WebAssembly:
                    webAssemblyState[entry.Key] = entry.Value.Item2;
                    break;
                default:
                    throw new InvalidOperationException("Invalid state direction to store.");
            }
        }
        if (webAssemblyState.Count != 0)
        {
            PersistedState = Convert.ToBase64String(SerializeState(webAssemblyState));
        }

        if (serverState.Count != 0)
        {
            PersistedProtectedState = Convert.ToBase64String(SerializeAndProtectState(serverState));
        }

        return Task.CompletedTask;
    }
}
