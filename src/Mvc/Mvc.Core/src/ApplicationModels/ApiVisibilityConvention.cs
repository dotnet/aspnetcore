// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// A <see cref="IActionModelConvention"/> that sets Api Explorer visibility.
/// </summary>
public class ApiVisibilityConvention : IActionModelConvention
{
    /// <inheritdoc />
    public void Apply(ActionModel action)
    {
        if (!ShouldApply(action))
        {
            return;
        }

        if (action.Controller.ApiExplorer.IsVisible == null && action.ApiExplorer.IsVisible == null)
        {
            // Enable ApiExplorer for the action if it wasn't already explicitly configured.
            action.ApiExplorer.IsVisible = true;
        }
    }

    /// <summary>
    /// Determines if this instance of <see cref="IActionModelConvention"/> applies to a specified <paramref name="action"/>.
    /// </summary>
    /// <param name="action">The <see cref="ActionModel"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the convention applies, otherwise <see langword="false"/>.
    /// Derived types may override this method to selectively apply this convention.
    /// </returns>
    protected virtual bool ShouldApply(ActionModel action) => true;
}
