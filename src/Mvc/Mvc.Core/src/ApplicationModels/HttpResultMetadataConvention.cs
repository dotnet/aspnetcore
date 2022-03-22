// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IActionModelConvention"/> that adds metadata to actions returning <see cref="IResult"/>.
/// </summary>
/// <remarks>
/// The following metadata are applied:
/// <list type="number">
/// <item>A <see cref="ProducesAttribute"/> with <c>application/json</c> content-type.</item>
/// </list>
/// </remarks>
internal class HttpResultMetadataConvention : IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        if (!ShouldApply(action))
        {
            return;
        }

        AddProducesAttribute(action);
    }

    /// <summary>
    /// Determines if this instance of <see cref="IActionModelConvention"/> applies to a specified <paramref name="action"/>.
    /// </summary>
    /// <param name="action">The <see cref="ActionModel"/>.</param>
    /// <returns>
    /// <see langword="true"/> if the convention applies, otherwise <see langword="false"/>.
    /// Derived types may override this method to selectively apply this convention.
    /// </returns>
    protected virtual bool ShouldApply(ActionModel action) => typeof(IResult).IsAssignableFrom(action.ActionMethod.ReturnType);

    private static void AddProducesAttribute(ActionModel action)
    {
        // Currently all IResult is written using HttpResponse.WriteAsJsonAsync<TValue>(T).
        action.Filters.Add(new ProducesAttribute("application/json"));
    }
}
