// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Cors.Infrastructure;

namespace Microsoft.AspNetCore.Cors
{
    /// <summary>
    /// Metadata that provides a CORS policy.
    /// </summary>
    public class CorsPolicyMetadata : ICorsPolicyMetadata
    {
        public CorsPolicyMetadata(CorsPolicy policy)
        {
            Policy = policy;
        }

        /// <summary>
        /// The policy which needs to be applied.
        /// </summary>
        public CorsPolicy Policy { get; }
    }
}
