// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
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
        /// Constraints with this value are considered less important than ones with 
        /// <see cref="RequireKey"/>
        /// </summary>
        CatchAll,
    }
}
