// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the result of an authentication operation.
/// </summary>
/// <typeparam name="TRemoteAuthenticationState">The type of the preserved state during the authentication operation.</typeparam>
public class RemoteAuthenticationResult<TRemoteAuthenticationState> where TRemoteAuthenticationState : RemoteAuthenticationState
{
    /// <summary>
    /// Gets or sets the status of the authentication operation. The status can be one of <see cref="RemoteAuthenticationStatus"/>.
    /// </summary>
    public RemoteAuthenticationStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the error message of a failed authentication operation.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the preserved state of a successful authentication operation.
    /// </summary>
    public TRemoteAuthenticationState? State { get; set; }
}
