// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Features
{
    /// <summary>
    /// A connection feature allowing middleware to stop counting connections towards <see cref="KestrelServerLimits.MaxConcurrentConnections"/>.
    /// This is used by Kestrel internally to stop counting upgraded connections towards this limit.
    /// </summary>
    public interface IDecrementConcurrentConnectionCountFeature
    {
        /// <summary>
        /// Idempotent method to stop counting a connection towards <see cref="KestrelServerLimits.MaxConcurrentConnections"/>.
        /// </summary>
        void ReleaseConnection();
    }
}
