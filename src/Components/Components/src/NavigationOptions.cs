// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Additional options for navigating to another URI
    /// </summary>
    [Flags]
    public enum NavigationOptions
    {
        /// <summary>
        /// Use default options
        /// </summary>
        None = 0,
        /// <summary>
        /// Bypasses client-side routing and forces the browser to load the new page from the server, whether or not the URI would normally be handled by the client-side router.
        /// </summary>
        ForceLoad = 1,
        /// <summary>
        /// Indicates that the current history entry should be replaced, instead of pushing the new uri onto the browser history stack.
        /// </summary>
        ReplaceHistoryEntry = 2
    }
}
