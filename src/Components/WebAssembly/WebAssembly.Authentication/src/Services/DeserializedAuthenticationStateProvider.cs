// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DeserializedAuthenticationStateProvider : AuthenticationStateProvider
{
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
