// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

internal sealed class AuthenticationStateSerializer : IHostEnvironmentAuthenticationStateProvider, IDisposable
{
    // Do not change. This must match all versions of the server-side DeserializedAuthenticationStateProvider.PersistenceKey.
    internal const string PersistenceKey = $"__internal__{nameof(AuthenticationState)}";

    private readonly PersistentComponentState _state;
    private readonly Func<AuthenticationState, ValueTask<AuthenticationStateData?>> _serializeCallback;
    private readonly PersistingComponentStateSubscription _subscription;

    private Task<AuthenticationState>? _authenticationStateTask;

    public AuthenticationStateSerializer(PersistentComponentState persistentComponentState, IOptions<AuthenticationStateSerializationOptions> options)
    {
        _state = persistentComponentState;
        _serializeCallback = options.Value.SerializationCallback;
        _subscription = persistentComponentState.RegisterOnPersisting(OnPersistingAsync, RenderMode.InteractiveWebAssembly);
    }

    private async Task OnPersistingAsync()
    {
        if (_authenticationStateTask is null)
        {
            throw new InvalidOperationException($"{nameof(SetAuthenticationState)} must be called before the {nameof(PersistentComponentState)}.{nameof(PersistentComponentState.RegisterOnPersisting)} callback.");
        }

        var authenticationStateData = await _serializeCallback(await _authenticationStateTask);
        if (authenticationStateData is not null)
        {
            _state.PersistAsJson(PersistenceKey, authenticationStateData);
        }
    }

    /// <inheritdoc />
    public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        _authenticationStateTask = authenticationStateTask ?? throw new ArgumentNullException(nameof(authenticationStateTask));
    }

    public void Dispose()
    {
        _subscription.Dispose();
    }
}
