// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc.ApiExplorer;

/// <summary>
/// Provides a set of possible content types than can be consumed by the action.
/// </summary>
public interface IApiRequestMetadataProvider : IFilterMetadata
{
    /// <summary>
    /// Configures a collection of allowed content types which can be consumed by the action.
    /// </summary>
    /// <param name="contentTypes">The <see cref="MediaTypeCollection"/></param>
    void SetContentTypes(MediaTypeCollection contentTypes);
}
