// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter which sets the appropriate headers related to Response caching.
/// </summary>
internal interface IResponseCacheFilter : IFilterMetadata
{
}
