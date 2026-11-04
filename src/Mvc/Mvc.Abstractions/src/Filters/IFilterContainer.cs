// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A filter that requires a reference back to the <see cref="IFilterFactory"/> that created it.
/// </summary>
public interface IFilterContainer
{
    /// <summary>
    /// The <see cref="IFilterFactory"/> that created this filter instance.
    /// </summary>
    IFilterMetadata FilterDefinition { get; set; }
}
