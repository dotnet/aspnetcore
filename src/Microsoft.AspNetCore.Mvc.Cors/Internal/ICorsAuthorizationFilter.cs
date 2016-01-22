// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Cors.Internal
{
    /// <summary>
    /// A filter which can be used to enable/disable cors support for a resource.
    /// </summary>
    public interface ICorsAuthorizationFilter : IAsyncAuthorizationFilter, IOrderedFilter
    {
    }
}
