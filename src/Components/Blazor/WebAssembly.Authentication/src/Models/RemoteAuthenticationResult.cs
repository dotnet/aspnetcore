// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents the result of an authentication operation.
    /// </summary>
    /// <typeparam name="TState">The type of the preserved state during the authentication operation.</typeparam>
    public class RemoteAuthenticationResult<TState> where TState : class
    {
        /// <summary>
        /// Gets or sets the status of the authentication operation. The status can be one of <see cref="RemoteAuthenticationStatus"/>.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the error message of a failed authentication operation.
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the preserved state of a successful authentication operation.
        /// </summary>
        public TState State { get; set; }
    }
}
