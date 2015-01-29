// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNet.Mvc
{
    /// <summary>
    /// An <see cref="IActionFilter"/> which sets the appropriate headers related to Response caching.
    /// </summary>
    public interface IResponseCacheFilter : IActionFilter
    {
    }
}