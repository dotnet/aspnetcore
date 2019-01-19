// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// Modifies the URL matching behavior for a <see cref="NavLink"/>.
    /// </summary>
    public enum NavLinkMatch
    {
        /// <summary>
        /// Specifies that the <see cref="NavLink"/> should be active when it matches any prefix
        /// of the current URL.
        /// </summary>
        Prefix,

        /// <summary>
        /// Specifies that the <see cref="NavLink"/> should be active when it matches the entire
        /// current URL.
        /// </summary>
        All,
    }
}
