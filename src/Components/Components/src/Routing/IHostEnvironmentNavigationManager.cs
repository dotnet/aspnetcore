// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// An optional interface for <see cref="NavigationManager" /> implementations that must be initialized
    /// by the host.
    /// </summary>
    public interface IHostEnvironmentNavigationManager
    {
        /// <summary>
        /// Initializes the <see cref="NavigationManager" />.
        /// </summary>
        /// <param name="absoluteUri">The absolute URI.</param>
        /// <param name="baseUri">The base URI.</param>
        void Initialize(string absoluteUri, string baseUri);
    }
}
