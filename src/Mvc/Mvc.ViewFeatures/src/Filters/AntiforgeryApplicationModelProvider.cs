// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Linq;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.Core.Filters;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels;

internal sealed class AntiforgeryApplicationModelProvider(IOptions<MvcOptions> mvcOptions, ILogger<AntiforgeryMiddlewareAuthorizationFilter> logger) : IApplicationModelProvider
{
    private readonly MvcOptions _mvcOptions = mvcOptions.Value;
    private readonly AntiforgeryMiddlewareAuthorizationFilter AntiforgeryMiddlewareAuthorizationFilter = new(logger);

    public int Order => -1000 + 10;

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_mvcOptions.EnableEndpointRouting)
        {
            return;
        }

        foreach (var controllerModel in context.Result.Controllers)
        {
            var controllerFilterAdded = false;
            if (HasValidAntiforgeryMetadata(controllerModel.Attributes, controllerModel.Filters))
            {
                controllerModel.Filters.Add(AntiforgeryMiddlewareAuthorizationFilter);
                controllerFilterAdded = true;
            }

            foreach (var actionModel in controllerModel.Actions)
            {
                if (HasValidAntiforgeryMetadata(actionModel.Attributes, actionModel.Filters) && !controllerFilterAdded)
                {
                    actionModel.Filters.Add(AntiforgeryMiddlewareAuthorizationFilter);
                }
            }
        }
    }

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        // Intentionally empty.
    }

    private static bool HasValidAntiforgeryMetadata(IReadOnlyList<object> attributes, IList<IFilterMetadata> filters)
    {
        var antiforgeryMetadata = attributes.OfType<IAntiforgeryMetadata>();
        var antiforgeryAttribute = filters.OfType<ValidateAntiForgeryTokenAttribute>().FirstOrDefault();
        if (antiforgeryAttribute is not null && antiforgeryMetadata.Any())
        {
            throw new InvalidOperationException($"Cannot apply [{nameof(ValidateAntiForgeryTokenAttribute)}] and [{nameof(RequireAntiforgeryTokenAttribute)}] at the same time.");
        }
        return antiforgeryMetadata.Any();
    }
}
