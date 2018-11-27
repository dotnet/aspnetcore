// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// A filter which sets the appropriate headers related to Response caching.
    /// </summary>
    public interface IResponseCacheFilter : IFilterMetadata
    {
    }
}