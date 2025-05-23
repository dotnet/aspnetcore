// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class DeserializedAuthenticationStateProvider : AuthenticationStateProvider
{
    private static readonly Task<AuthenticationState> _defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    private readonly Task<AuthenticationState> _authenticationStateTask;

    [SupplyParameterFromPersistentComponentState]
    public AuthenticationStateData? CurrentAuthenticationState { get; set; }

    public DeserializedAuthenticationStateProvider(IOptions<AuthenticationStateDeserializationOptions> options)
    {
        _authenticationStateTask = CurrentAuthenticationState is not null
            ? options.Value.DeserializationCallback(CurrentAuthenticationState)
            : _defaultUnauthenticatedTask;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authenticationStateTask;
}
