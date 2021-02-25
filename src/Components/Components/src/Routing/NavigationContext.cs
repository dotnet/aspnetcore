// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Components.Routing
{
    /// <summary>
    /// Provides information about the current asynchronous navigation event
    /// including the target path and the cancellation token.
    /// </summary>
    public sealed class NavigationContext
    {
        internal NavigationContext(string path, CancellationToken cancellationToken)
        {
            Path = path;
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// The target path for the navigation.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// The <see cref="CancellationToken"/> to use to cancel navigation.
        /// </summary>
        public CancellationToken CancellationToken { get; }
    }
}
