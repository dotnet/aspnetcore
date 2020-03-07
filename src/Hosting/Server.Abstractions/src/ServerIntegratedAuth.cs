// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Hosting.Server
{
    /// <summary>
    /// Used by servers to advertise if they support integrated Windows authentication, if it's enabled, and it's scheme.
    /// </summary>
    public class ServerIntegratedAuth : IServerIntegratedAuth
    {
        /// <summary>
        /// Indicates if integrated Windows authentication is enabled for the current application instance.
        /// </summary>
        public bool IsEnabled { get; set;}

        /// <summary>
        /// The name of the authentication scheme for the server authentication handler.
        /// </summary>
        public string AuthenticationScheme { get; set; }
    }
}
