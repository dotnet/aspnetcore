// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Additional options for navigating to another URI
    /// </summary>
    public class NavigationOptions
    {
        /// <summary>
        ///If true, bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.
        /// </summary>
        public bool ForceLoad { get; set; }
        /// <summary>
        /// If true, will replace the uri in the current browser history state, instead of pushing the new uri onto the browser history stack.
        /// </summary>
        public bool Replace { get; set; }
    }
}
