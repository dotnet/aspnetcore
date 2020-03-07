// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Represents HTTP method metadata used during routing.
    /// </summary>
    public interface IHttpMethodMetadata
    {
        /// <summary>
        /// Returns a value indicating whether the associated endpoint should accept CORS preflight requests.
        /// </summary>
        bool AcceptCorsPreflight { get; }

        /// <summary>
        /// Returns a read-only collection of HTTP methods used during routing.
        /// An empty collection means any HTTP method will be accepted.
        /// </summary>
        IReadOnlyList<string> HttpMethods { get; }
    }
}
