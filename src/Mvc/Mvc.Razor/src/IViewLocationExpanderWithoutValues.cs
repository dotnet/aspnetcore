// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.Razor;

/// <summary>
/// Implemented by framework <see cref="IViewLocationExpander"/> implementations whose
/// <see cref="IViewLocationExpander.PopulateValues(ViewLocationExpanderContext)"/> does nothing, so that
/// <see cref="RazorViewEngine"/> can look up cached view locations without populating expander values.
/// </summary>
internal interface IViewLocationExpanderWithoutValues
{
    /// <summary>
    /// Gets a value that determines whether <see cref="IViewLocationExpander.PopulateValues(ViewLocationExpanderContext)"/>
    /// does nothing for this instance.
    /// </summary>
    bool DoesNotPopulateValues { get; }
}
