// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
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
}
