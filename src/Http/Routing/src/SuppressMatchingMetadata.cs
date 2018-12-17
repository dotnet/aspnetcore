// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing
{
    /// <summary>
    /// Metadata used to prevent URL matching. If <see cref="SuppressMatching"/> is <c>true</c> the
    /// associated endpoint will not be considered for URL matching.
    /// </summary>
    public sealed class SuppressMatchingMetadata : ISuppressMatchingMetadata
    {
        /// <summary>
        /// Gets a value indicating whether the assocated endpoint should be used for URL matching.
        /// </summary>
        public bool SuppressMatching => true;
    }
}
