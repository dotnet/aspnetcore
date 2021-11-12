// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Http.Abstractions.Metadata;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

/// <summary>
/// An <see cref="IApplicationModelProvider"/> that removes antiforgery filters that appears as endpoint metadata.
/// </summary>
internal sealed class AntiforgeryApplicationModelProvider : IApplicationModelProvider
{
    private readonly MvcOptions _mvcOptions;

    public AntiforgeryApplicationModelProvider(IOptions<MvcOptions> mvcOptions)
    {
        _mvcOptions = mvcOptions.Value;
    }

    // Run late in the pipeline so that we can pick up user configured AntiforgeryTokens.
    public int Order { get; } = 1000;

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
    }

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        if (!_mvcOptions.EnableEndpointRouting)
        {
            return;
        }

        foreach (var controller in context.Result.Controllers)
        {
            RemoveAntiforgeryFilters(controller.Filters, controller.Selectors);

            foreach (var action in controller.Actions)
            {
                RemoveAntiforgeryFilters(action.Filters, action.Selectors);
            }
        }
    }

    private static void RemoveAntiforgeryFilters(IList<IFilterMetadata> filters, IList<SelectorModel> selectorModels)
    {
        for (var i = filters.Count - 1; i >= 0; i--)
        {
            if (filters[i] is IAntiforgeryMetadata antiforgeryMetadata &&
                selectorModels.All(s => s.EndpointMetadata.Contains(antiforgeryMetadata)))
            {
                filters.RemoveAt(i);
            }
        }
    }
}
