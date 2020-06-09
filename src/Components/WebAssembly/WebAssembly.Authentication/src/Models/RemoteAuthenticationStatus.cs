// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    /// <summary>
    /// Represents the status of an authentication operation.
    /// </summary>
    public enum RemoteAuthenticationStatus
    {
        /// <summary>
        /// The application is going to be redirected.
        /// </summary>
        Redirect,

        /// <summary>
        /// The authentication operation completed successfully.
        /// </summary>
        Success,

        /// <summary>
        /// There was an error performing the authentication operation.
        /// </summary>
        Failure,

        /// <summary>
        /// The operation in the current navigation context has completed. This signals that the application running on the
        /// current browser context is about to be shut down and no other work is required.
        /// </summary>
        OperationCompleted,
    }
}
