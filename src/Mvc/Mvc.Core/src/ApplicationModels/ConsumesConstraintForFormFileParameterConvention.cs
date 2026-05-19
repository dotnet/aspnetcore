// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IActionModelConvention"/> that adds a <see cref="ConsumesAttribute"/> with <c>multipart/form-data</c>
/// to controllers containing form file (<see cref="BindingSource.FormFile"/>) parameters.
/// </summary>
public class ConsumesConstraintForFormFileParameterConvention : IActionModelConvention
{
    /// <inheritdoc />
    public void Apply(ActionModel action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (!ShouldApply(action))
        {
            return;
        }

        AddMultipartFormDataConsumesAttribute(action);
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

    // Internal for unit testing
    internal static void AddMultipartFormDataConsumesAttribute(ActionModel action)
    {
        // Add a ConsumesAttribute if the request does not explicitly specify one.
        if (action.Filters.OfType<IConsumesActionConstraint>().Any())
        {
            return;
        }

        foreach (var parameter in action.Parameters)
        {
            var bindingSource = parameter.BindingInfo?.BindingSource;
            if (bindingSource == BindingSource.FormFile)
            {
                // If an controller accepts files, it must accept multipart/form-data.
                action.Filters.Add(new ConsumesAttribute("multipart/form-data"));
                return;
            }
        }
    }
}
