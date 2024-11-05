// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Server;

/// <summary>
/// Provides options for configuring the JSON serialization of the <see cref="AuthenticationState"/> provided by the server's <see cref="AuthenticationStateProvider"/>
/// to the WebAssembly client using <see cref="PersistentComponentState"/>.
/// </summary>
public class AuthenticationStateSerializationOptions
{
    /// <summary>
    /// Default constructor for <see cref="AuthenticationStateSerializationOptions"/>.
    /// </summary>
    public AuthenticationStateSerializationOptions()
    {
        SerializationCallback = SerializeAuthenticationStateAsync;
    }

    /// <summary>
    /// If <see langword="true"/>, the default <see cref="SerializationCallback"/> will serialize all claims; otherwise, it will only serialize
    /// the <see cref="ClaimsIdentity.NameClaimType"/> and <see cref="ClaimsIdentity.RoleClaimType"/> claims.
    /// </summary>
    public bool SerializeAllClaims { get; set; }

    /// <summary>
    /// Default implementation for converting the server's <see cref="AuthenticationState"/> to an <see cref="AuthenticationStateData"/> object
    /// for JSON serialization to the client using <see cref="PersistentComponentState"/>."/>
    /// </summary>
    public Func<AuthenticationState, ValueTask<AuthenticationStateData?>> SerializationCallback { get; set; }

    private ValueTask<AuthenticationStateData?> SerializeAuthenticationStateAsync(AuthenticationState authenticationState)
    {
        AuthenticationStateData? data = null;

        if (authenticationState.User.Identity?.IsAuthenticated ?? false)
        {
            data = new AuthenticationStateData();

            if (authenticationState.User.Identities.FirstOrDefault() is { } identity)
            {
                data.NameClaimType = identity.NameClaimType;
                data.RoleClaimType = identity.RoleClaimType;
            }

            if (SerializeAllClaims)
            {
                foreach (var claim in authenticationState.User.Claims)
                {
                    data.Claims.Add(new(claim));
                }
            }
            else
            {
                if (authenticationState.User.FindFirst(data.NameClaimType) is { } nameClaim)
                {
                    data.Claims.Add(new(nameClaim));
                }

                foreach (var roleClaim in authenticationState.User.FindAll(data.RoleClaimType))
                {
                    data.Claims.Add(new(roleClaim));
                }
            }
        }

        return ValueTask.FromResult(data);
    }
}
