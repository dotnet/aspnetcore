// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching
{
    public abstract class PolicyJumpTable
    {
        public abstract int GetDestination(HttpContext httpContext);

        internal virtual string DebuggerToString()
        {
            return GetType().Name;
        }
    }
}
