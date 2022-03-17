// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// An <see cref="RemoteAuthenticatorViewCore{TAuthenticationState}"/> that uses <see cref="RemoteAuthenticationState"/> as the
/// state to be persisted across authentication operations.
/// </summary>
public class RemoteAuthenticatorView : RemoteAuthenticatorViewCore<RemoteAuthenticationState>
{
    /// <summary>
    /// Initializes a new instance of <see cref="RemoteAuthenticatorView"/>.
    /// </summary>
    public RemoteAuthenticatorView() => AuthenticationState = new RemoteAuthenticationState();
}
