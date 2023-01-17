// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.Filters;

/// <summary>
/// A context for resource filters, specifically <see cref="IResourceFilter.OnResourceExecuting"/> and
/// <see cref="IAsyncResourceFilter.OnResourceExecutionAsync"/> calls.
/// </summary>
public class ResourceExecutingContext : FilterContext
{
    /// <summary>
    /// Creates a new <see cref="ResourceExecutingContext"/>.
    /// </summary>
    /// <param name="actionContext">The <see cref="ActionContext"/>.</param>
    /// <param name="filters">The list of <see cref="IFilterMetadata"/> instances.</param>
    /// <param name="valueProviderFactories">The list of <see cref="IValueProviderFactory"/> instances.</param>
    public ResourceExecutingContext(
        ActionContext actionContext,
        IList<IFilterMetadata> filters,
        IList<IValueProviderFactory> valueProviderFactories)
        : base(actionContext, filters)
    {
        ArgumentNullException.ThrowIfNull(valueProviderFactories);

        ValueProviderFactories = valueProviderFactories;
    }

    /// <summary>
    /// Gets or sets the result of the action to be executed.
    /// </summary>
    /// <remarks>
    /// Setting <see cref="Result"/> to a non-<c>null</c> value inside a resource filter will
    /// short-circuit execution of additional resource filters and the action itself.
    /// </remarks>
    public virtual IActionResult? Result { get; set; }

    /// <summary>
    /// Gets the list of <see cref="IValueProviderFactory"/> instances used by model binding.
    /// </summary>
    public IList<IValueProviderFactory> ValueProviderFactories { get; }
}
