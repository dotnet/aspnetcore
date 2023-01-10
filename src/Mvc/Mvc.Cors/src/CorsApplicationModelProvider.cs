// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Cors;

internal sealed class CorsApplicationModelProvider : IApplicationModelProvider
{
    private readonly MvcOptions _mvcOptions;

    public CorsApplicationModelProvider(IOptions<MvcOptions> mvcOptions)
    {
        _mvcOptions = mvcOptions.Value;
    }

    public int Order => -1000 + 10;

    public void OnProvidersExecuted(ApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Intentionally empty.
    }

    public void OnProvidersExecuting(ApplicationModelProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!_mvcOptions.EnableEndpointRouting)
        {
            ConfigureCorsFilters(context);
        }
    }

    private static void ConfigureCorsFilters(ApplicationModelProviderContext context)
    {
        var isCorsEnabledGlobally = context.Result.Filters.OfType<ICorsAuthorizationFilter>().Any() ||
                        context.Result.Filters.OfType<CorsAuthorizationFilterFactory>().Any();

        foreach (var controllerModel in context.Result.Controllers)
        {
            var enableCors = controllerModel.Attributes.OfType<IEnableCorsAttribute>().FirstOrDefault();
            if (enableCors != null)
            {
                controllerModel.Filters.Add(new CorsAuthorizationFilterFactory(enableCors.PolicyName));
            }

            var disableCors = controllerModel.Attributes.OfType<IDisableCorsAttribute>().FirstOrDefault();
            if (disableCors != null)
            {
                controllerModel.Filters.Add(new DisableCorsAuthorizationFilter());
            }

            var corsOnController = enableCors != null || disableCors != null || controllerModel.Filters.OfType<ICorsAuthorizationFilter>().Any();

            foreach (var actionModel in controllerModel.Actions)
            {
                enableCors = actionModel.Attributes.OfType<IEnableCorsAttribute>().FirstOrDefault();
                if (enableCors != null)
                {
                    actionModel.Filters.Add(new CorsAuthorizationFilterFactory(enableCors.PolicyName));
                }

                disableCors = actionModel.Attributes.OfType<IDisableCorsAttribute>().FirstOrDefault();
                if (disableCors != null)
                {
                    actionModel.Filters.Add(new DisableCorsAuthorizationFilter());
                }

                var corsOnAction = enableCors != null || disableCors != null || actionModel.Filters.OfType<ICorsAuthorizationFilter>().Any();

                if (isCorsEnabledGlobally || corsOnController || corsOnAction)
                {
                    ConfigureCorsActionConstraint(actionModel);
                }
            }
        }
    }

    private static void ConfigureCorsActionConstraint(ActionModel actionModel)
    {
        var selectors = actionModel.Selectors;
        // Read interface .Count once rather than per iteration
        var selectorsCount = selectors.Count;

        for (var i = 0; i < selectorsCount; i++)
        {
            var selectorModel = selectors[i];

            var actionConstraints = selectorModel.ActionConstraints;
            // Read interface .Count once rather than per iteration
            var actionConstraintsCount = actionConstraints.Count;
            for (var j = 0; j < actionConstraintsCount; j++)
            {
                if (actionConstraints[j] is HttpMethodActionConstraint httpConstraint)
                {
                    actionConstraints[j] = new CorsHttpMethodActionConstraint(httpConstraint);
                }
            }
        }
    }

}
