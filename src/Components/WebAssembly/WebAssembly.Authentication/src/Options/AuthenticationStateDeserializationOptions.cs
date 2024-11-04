// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Provides options for configuring the JSON deserialization of the client's <see cref="AuthenticationState"/> from the server using <see cref="PersistentComponentState"/>.
/// </summary>
public sealed class AuthenticationStateDeserializationOptions
{
    private static readonly Task<AuthenticationState> _defaultUnauthenticatedStateTask =
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));

    /// <summary>
    /// Default implementation for converting the <see cref="AuthenticationStateData"/> that was JSON deserialized from the server
    /// using <see cref="PersistentComponentState"/> to an <see cref="AuthenticationState"/> object to be returned by the WebAssembly
    /// client's <see cref="AuthenticationStateProvider"/>.
    /// </summary>
    public Func<AuthenticationStateData?, Task<AuthenticationState>> DeserializationCallback { get; set; } = DeserializeAuthenticationStateAsync;

    private static Task<AuthenticationState> DeserializeAuthenticationStateAsync(AuthenticationStateData? authenticationStateData)
    {
        if (authenticationStateData is null)
        {
            return _defaultUnauthenticatedStateTask;
        }

        return Task.FromResult(
            new AuthenticationState(new ClaimsPrincipal(
                new ClaimsIdentity(authenticationStateData.Claims.Select(c => new Claim(c.Type, c.Value)),
                    authenticationType: nameof(DeserializedAuthenticationStateProvider),
                    nameType: authenticationStateData.NameClaimType,
                    roleType: authenticationStateData.RoleClaimType))));
    }
}
