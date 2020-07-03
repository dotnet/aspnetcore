// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.WebAssembly.Http
{
    /// <summary>
    /// The mode of the request. This is used to determine if cross-origin requests lead to valid responses
    /// </summary>
    public enum BrowserRequestMode
    {
        /// <summary>
        /// If a request is made to another origin with this mode set, the result is simply an error
        /// </summary>
        SameOrigin,

        /// <summary>
        /// Prevents the method from being anything other than HEAD, GET or POST, and the headers from
        /// being anything other than simple headers.
        /// </summary>
        NoCors,

        /// <summary>
        /// Allows cross-origin requests, for example to access various APIs offered by 3rd party vendors.
        /// </summary>
        Cors,

        /// <summary>
        /// A mode for supporting navigation.
        /// </summary>
        Navigate,
    }
}
