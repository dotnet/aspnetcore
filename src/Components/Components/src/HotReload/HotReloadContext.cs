// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.HotReload
{
    /// <summary>
    /// A context that indicates when a component is being rendered after a hot reload is applied to the application.
    /// </summary>
    public sealed class HotReloadContext
    {
        /// <summary>
        /// Gets a value that indicates if the application is re-rendering in response to a hot-reload change.
        /// </summary>
        public bool IsHotReloading { get; internal set; }
    }
}
