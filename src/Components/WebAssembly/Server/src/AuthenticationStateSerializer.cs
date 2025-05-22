// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

internal sealed class AuthenticationStateSerializer : IHostEnvironmentAuthenticationStateProvider
{
    // Do not change. This must match all versions of the server-side DeserializedAuthenticationStateProvider.PersistenceKey.
    internal const string PersistenceKey = $"__internal__{nameof(AuthenticationState)}";

    private readonly Func<AuthenticationState, ValueTask<AuthenticationStateData?>> _serializeCallback;

    private Task<AuthenticationState>? _authenticationStateTask;
    private AuthenticationStateData? _authenticationStateData;

    [SupplyParameterFromPersistentComponentState]
    public AuthenticationStateData? AuthStateData
    {
        get => _authenticationStateData;
        set => _authenticationStateData = value;
    }

    public AuthenticationStateSerializer(IOptions<AuthenticationStateSerializationOptions> options)
    {
        _serializeCallback = options.Value.SerializationCallback;
    }

    /// <inheritdoc />
    public async void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        _authenticationStateTask = authenticationStateTask ?? throw new ArgumentNullException(nameof(authenticationStateTask));
        
        if (_authenticationStateTask is not null)
        {
            AuthStateData = await _serializeCallback(await _authenticationStateTask);
        }
    }
}
