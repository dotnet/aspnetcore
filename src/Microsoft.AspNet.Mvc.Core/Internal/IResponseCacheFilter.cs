// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc.Filters;

namespace Microsoft.AspNet.Mvc.Internal
{
    /// <summary>
    /// An <see cref="IActionFilter"/> which sets the appropriate headers related to Response caching.
    /// </summary>
    public interface IResponseCacheFilter : IActionFilter
    {
    }
}