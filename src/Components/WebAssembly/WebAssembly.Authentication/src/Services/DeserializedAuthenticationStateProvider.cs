// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DeserializedAuthenticationStateProvider : AuthenticationStateProvider
{
    // restoring part is on DeserializedAuthenticationStateProvider but persisting part is on AuthenticationStateSerializer
    // how can we make the key the same if these are two different classes in different assemblies?
    // should we merge them and move to the Shared folder? Or should we allow passing a custom key to [SupplyParameterFromPersistentComponentState] attribute?
    private readonly Func<AuthenticationStateData?, Task<AuthenticationState>> _deserializeCallback;

    [SupplyParameterFromPersistentComponentState]
    public AuthenticationStateData? CurrentAuthenticationState { get; set; }

    private static readonly Task<AuthenticationState> _defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public DeserializedAuthenticationStateProvider(IOptions<AuthenticationStateDeserializationOptions> options)
    {
        _deserializeCallback = options.Value.DeserializationCallback;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (CurrentAuthenticationState is null)
        {
            return _defaultUnauthenticatedTask;
        }
        var authenticationState = _deserializeCallback(CurrentAuthenticationState);
        return authenticationState;
    }
}
