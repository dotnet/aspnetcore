// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.TagHelpers
{
    /// <summary>
    /// The way to persist the component application state.
    /// </summary>
    public enum PersistenceMode
    {
        /// <summary>
        /// The state is persisted for a Blazor Server application.
        /// </summary>
        Server,

        /// <summary>
        /// The state is persisted for a Blazor WebAssembly application.
        /// </summary>
        WebAssembly
    }
}
