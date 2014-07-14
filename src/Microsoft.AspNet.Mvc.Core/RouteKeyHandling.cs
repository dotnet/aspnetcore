// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    // This needs more thought, the intent is that we would be able to cache over this constraint
    // without running the accept method.
    public enum RouteKeyHandling
    {
        /// <summary>
        /// Requires that the key will be in the route values, and that the content matches.
        /// </summary>
        RequireKey,

        /// <summary>
        /// Requires that the key will not be in the route values.
        /// </summary>
        DenyKey,

        /// <summary>
        /// Requires that the key will be in the route values, but ignore the content.
        /// </summary>
        CatchAll,
    }
}
