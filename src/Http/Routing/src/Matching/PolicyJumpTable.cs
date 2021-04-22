// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    /// <summary>
    /// Supports retrieving endpoints that fulfill a certain matcher policy.
    /// </summary>
    public abstract class PolicyJumpTable
    {
        /// <summary>
        /// Returns the destination for a given <paramref name="httpContext"/> in the current jump table.
        /// </summary>
        /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
        public abstract int GetDestination(HttpContext httpContext);

        internal virtual string DebuggerToString()
        {
            return GetType().Name;
        }
    }
}
