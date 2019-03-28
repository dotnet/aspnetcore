// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Services
{
    /// <summary>
    /// Provides information about the environment in which components are executing.
    /// </summary>
    public interface IComponentContext
    {
        /// <summary>
        /// Gets a flag to indicate whether there is an active connection to the user's display.
        /// </summary>
        /// <example>During prerendering, the value will always be false.</example>
        /// <example>During server-side execution, the value can be true or false depending on whether there is an active SignalR connection.</example>
        /// <example>During client-side execution, the value will always be true.</example>
        bool IsConnected { get; }
    }
}
