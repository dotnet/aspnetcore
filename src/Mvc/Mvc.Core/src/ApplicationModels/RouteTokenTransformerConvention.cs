// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IActionModelConvention"/> that sets attribute routing token replacement
/// to use the specified <see cref="IOutboundParameterTransformer"/> on <see cref="ActionModel"/>.
/// This convention does not effect Razor page routes.
/// </summary>
public class RouteTokenTransformerConvention : IActionModelConvention
{
    private readonly IOutboundParameterTransformer _parameterTransformer;

    /// <summary>
    /// Creates a new instance of <see cref="RouteTokenTransformerConvention"/> with the specified <see cref="IOutboundParameterTransformer"/>.
    /// </summary>
    /// <param name="parameterTransformer">The <see cref="IOutboundParameterTransformer"/> to use with attribute routing token replacement.</param>
    public RouteTokenTransformerConvention(IOutboundParameterTransformer parameterTransformer)
    {
        ArgumentNullException.ThrowIfNull(parameterTransformer);

        _parameterTransformer = parameterTransformer;
    }

    /// <inheritdoc />
    public void Apply(ActionModel action)
    {
        if (ShouldApply(action))
        {
            action.RouteParameterTransformer = _parameterTransformer;
        }
    }

    /// <summary>
    /// Called to determine whether an action should apply.
    /// </summary>
    /// <param name="action">The <see cref="ActionModel"/>.</param>
    /// <returns><see langword="true"/> if the the action should apply.</returns>
    protected virtual bool ShouldApply(ActionModel action) => true;
}
