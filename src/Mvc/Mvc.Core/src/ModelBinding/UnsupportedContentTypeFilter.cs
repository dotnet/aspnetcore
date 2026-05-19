// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.ModelBinding;

/// <summary>
/// A filter that scans for <see cref="UnsupportedContentTypeException"/> in the
/// <see cref="ActionContext.ModelState"/> and short-circuits the pipeline
/// with an Unsupported Media Type (415) response.
/// </summary>
public class UnsupportedContentTypeFilter : IActionFilter, IOrderedFilter
{
    /// <summary>
    /// Gets or sets the filter order. <see cref="IOrderedFilter.Order"/>.
    /// <para>
    /// Defaults to <c>-3000</c> to ensure it executes before <see cref="ModelStateInvalidFilter"/>.
    /// </para>
    /// </summary>
    public int Order { get; set; } = -3000;

    /// <inheritdoc />
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (HasUnsupportedContentTypeError(context))
        {
            context.Result = new UnsupportedMediaTypeResult();
        }
    }

    private static bool HasUnsupportedContentTypeError(ActionExecutingContext context)
    {
        var modelState = context.ModelState;
        if (modelState.IsValid)
        {
            return false;
        }

        foreach (var kvp in modelState)
        {
            var errors = kvp.Value.Errors;
            for (var i = 0; i < errors.Count; i++)
            {
                var error = errors[i];
                if (error.Exception is UnsupportedContentTypeException)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
