// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

internal sealed class AuthenticationStateSerializer : AuthenticationStateProvider, IHostEnvironmentAuthenticationStateProvider
{
    private readonly Func<AuthenticationState, ValueTask<AuthenticationStateData?>> _serializeCallback;

    [SupplyParameterFromPersistentComponentState]
    public AuthenticationStateData? CurrentAuthenticationState { get; set; }

    private static readonly Task<AuthenticationState> _defaultUnauthenticatedTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    public AuthenticationStateSerializer(IOptions<AuthenticationStateSerializationOptions> options)
    {
        _serializeCallback = options.Value.SerializationCallback;
    }

    /// <inheritdoc />
    public void SetAuthenticationState(Task<AuthenticationState> authenticationStateTask)
    {
        ArgumentNullException.ThrowIfNull(authenticationStateTask, nameof(authenticationStateTask));
        _ = SetAuthenticationStateAsync(authenticationStateTask);
    }

    private async Task SetAuthenticationStateAsync(Task<AuthenticationState> authenticationStateTask)
    {
        var authenticationState = await authenticationStateTask;
        CurrentAuthenticationState = await _serializeCallback(authenticationState);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => _defaultUnauthenticatedTask;
}
