// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Cors.Infrastructure
{
    /// <summary>
    /// An interface which can be used to identify a type which provides metadata needed for enabling CORS support.
    /// </summary>
    public interface ICorsPolicyMetadata : ICorsMetadata
    {
        /// <summary>
        /// The policy which needs to be applied.
        /// </summary>
        CorsPolicy Policy { get; }
    }
}
