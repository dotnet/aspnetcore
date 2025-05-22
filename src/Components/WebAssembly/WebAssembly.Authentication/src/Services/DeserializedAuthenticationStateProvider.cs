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
    public AuthenticationStateData? AuthStateData { get; set; }

    public DeserializedAuthenticationStateProvider(IOptions<AuthenticationStateDeserializationOptions> options)
    {
        _authenticationStateTask = AuthStateData is not null
            ? options.Value.DeserializationCallback(AuthStateData)
            : _defaultUnauthenticatedTask;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _authenticationStateTask;
}
