// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// An interface for filter metadata which can create an instance of an executable filter.
/// </summary>
public interface IFilterFactory : IFilterMetadata
{
    /// <summary>
    /// Gets a value that indicates if the result of <see cref="CreateInstance(IServiceProvider)"/>
    /// can be reused across requests.
    /// </summary>
    bool IsReusable { get; }

    /// <summary>
    /// Creates an instance of the executable filter.
    /// </summary>
    /// <param name="serviceProvider">The request <see cref="IServiceProvider"/>.</param>
    /// <returns>An instance of the executable filter.</returns>
    IFilterMetadata CreateInstance(IServiceProvider serviceProvider);
}
