// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the context during authentication operations.
/// </summary>
/// <typeparam name="TRemoteAuthenticationState"></typeparam>
public class RemoteAuthenticationContext<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState> where TRemoteAuthenticationState : RemoteAuthenticationState
{
    /// <summary>
    /// Gets or sets the url for the current authentication operation.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets the state instance for the current authentication operation.
    /// </summary>
    public TRemoteAuthenticationState? State { get; set; }

    /// <summary>
    /// Gets or sets the interaction request for the current authentication operation.
    /// </summary>
    public InteractiveRequestOptions? InteractiveRequest { get; set; }
}
