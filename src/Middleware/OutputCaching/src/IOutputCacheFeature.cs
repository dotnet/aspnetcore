// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.OutputCaching;

/// <summary>
/// A feature for configuring additional output cache options on the HTTP response.
/// </summary>
public interface IOutputCacheFeature
{
    /// <summary>
    /// Gets the cache context.
    /// </summary>
    OutputCacheContext Context { get; }
}
