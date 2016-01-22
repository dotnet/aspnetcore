// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc.Cors.Internal
{
    /// <summary>
    /// A filter which can be used to enable/disable cors support for a resource.
    /// </summary>
    public interface ICorsAuthorizationFilter : IAsyncAuthorizationFilter, IOrderedFilter
    {
    }
}
