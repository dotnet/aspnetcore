// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IPageRouteModelConvention"/> that sets page route resolution
/// to use the specified <see cref="IOutboundParameterTransformer"/> on <see cref="PageRouteModel"/>.
/// This convention does not effect controller action routes.
/// </summary>
public class PageRouteTransformerConvention : IPageRouteModelConvention
{
    private readonly IOutboundParameterTransformer _parameterTransformer;

    /// <summary>
    /// Creates a new instance of <see cref="PageRouteTransformerConvention"/> with the specified <see cref="IOutboundParameterTransformer"/>.
    /// </summary>
    /// <param name="parameterTransformer">The <see cref="IOutboundParameterTransformer"/> to use resolve page routes.</param>
    public PageRouteTransformerConvention(IOutboundParameterTransformer parameterTransformer)
    {
        ArgumentNullException.ThrowIfNull(parameterTransformer);

        _parameterTransformer = parameterTransformer;
    }

    /// <inheritdoc/>
    public void Apply(PageRouteModel model)
    {
        if (ShouldApply(model))
        {
            model.RouteParameterTransformer = _parameterTransformer;
        }
    }

    /// <summary>
    /// Called to determine if this convention should apply.
    /// </summary>
    /// <param name="action">The action in question.</param>
    /// <returns>Whether this convention should apply.</returns>
    protected virtual bool ShouldApply(PageRouteModel action) => true;
}
