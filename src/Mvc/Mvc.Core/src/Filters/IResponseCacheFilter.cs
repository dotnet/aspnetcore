// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Mvc.Filters
{
    /// <summary>
    /// A filter which sets the appropriate headers related to Response caching.
    /// </summary>
    internal interface IResponseCacheFilter : IFilterMetadata
    {
    }
}
