// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IActionModelConvention"/> that adds a <see cref="IFilterMetadata"/>
/// to <see cref="ActionModel"/> that responds to invalid <see cref="ActionContext.ModelState"/>
/// </summary>
public class InvalidModelStateFilterConvention : IActionModelConvention
{
    private readonly ModelStateInvalidFilterFactory _filterFactory = new ModelStateInvalidFilterFactory();

    /// <inheritdoc />
    public void Apply(ActionModel action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!ShouldApply(action))
        {
            return;
        }

        action.Filters.Add(_filterFactory);
    }

    /// <summary>
    /// Called to determine whether the action should apply.
    /// </summary>
    /// <param name="action">The action in question.</param>
    /// <returns><see langword="true"/> if the action should apply.</returns>
    protected virtual bool ShouldApply(ActionModel action) => true;
}
