// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents the context during authentication operations.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState"></typeparam>
    public class RemoteAuthenticationContext<TRemoteAuthenticationState> where TRemoteAuthenticationState : RemoteAuthenticationState
    {
        /// <summary>
        /// Gets or sets the url for the current authentication operation.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the state instance for the current authentication operation.
        /// </summary>
        public TRemoteAuthenticationState State { get; set; }
    }
}
