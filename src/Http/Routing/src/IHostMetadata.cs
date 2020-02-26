// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Represents host metadata used during routing.
    /// </summary>
    public interface IHostMetadata
    {
        /// <summary>
        /// Returns a read-only collection of hosts used during routing.
        /// Hosts will be Unicode rather than punycode, and may have a port.
        /// An empty collection means any host will be accepted.
        /// </summary>
        IReadOnlyList<string> Hosts { get; }
    }
}
