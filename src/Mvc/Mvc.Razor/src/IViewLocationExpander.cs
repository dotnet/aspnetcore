// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Specifies the contracts for a view location expander that is used by <see cref="RazorViewEngine"/> instances to
/// determine search paths for a view.
/// </summary>
/// <remarks>
/// Individual <see cref="IViewLocationExpander"/>s are invoked in two steps:
/// (1) <see cref="PopulateValues(ViewLocationExpanderContext)"/> is invoked and each expander
/// adds values that it would later consume as part of
/// <see cref="ExpandViewLocations(ViewLocationExpanderContext, IEnumerable{string})"/>.
/// The populated values are used to determine a cache key - if all values are identical to the last time
/// <see cref="PopulateValues(ViewLocationExpanderContext)"/> was invoked, the cached result
/// is used as the view location.
/// (2) If no result was found in the cache or if a view was not found at the cached location,
/// <see cref="ExpandViewLocations(ViewLocationExpanderContext, IEnumerable{string})"/> is invoked to determine
/// all potential paths for a view.
/// </remarks>
public interface IViewLocationExpander
{
    /// <summary>
    /// Invoked by a <see cref="RazorViewEngine"/> to determine the values that would be consumed by this instance
    /// of <see cref="IViewLocationExpander"/>. The calculated values are used to determine if the view location
    /// has changed since the last time it was located.
    /// </summary>
    /// <param name="context">The <see cref="ViewLocationExpanderContext"/> for the current view location
    /// expansion operation.</param>
    void PopulateValues(ViewLocationExpanderContext context);

    /// <summary>
    /// Invoked by a <see cref="RazorViewEngine"/> to determine potential locations for a view.
    /// </summary>
    /// <param name="context">The <see cref="ViewLocationExpanderContext"/> for the current view location
    /// expansion operation.</param>
    /// <param name="viewLocations">The sequence of view locations to expand.</param>
    /// <returns>A list of expanded view locations.</returns>
    IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context,
                                            IEnumerable<string> viewLocations);
}
