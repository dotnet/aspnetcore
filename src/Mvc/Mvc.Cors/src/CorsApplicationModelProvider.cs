// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Cors
{
    internal class CorsApplicationModelProvider : IApplicationModelProvider
    {
        private readonly MvcOptions _mvcOptions;

        public CorsApplicationModelProvider(IOptions<MvcOptions> mvcOptions)
        {
            _mvcOptions = mvcOptions.Value;
        }

        public int Order => -1000 + 10;

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Intentionally empty.
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_mvcOptions.EnableEndpointRouting)
            {
                // When doing endpoint routing, translate IEnableCorsAttribute to an HttpMethodMetadata with CORS enabled.
                ConfigureCorsEndpointMetadata(context.Result);
            }
            else
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

        private static void ConfigureCorsEndpointMetadata(ApplicationModel applicationModel)
        {
            foreach (var controller in applicationModel.Controllers)
            {
                var corsOnController = controller.Attributes.OfType<IDisableCorsAttribute>().Any() ||
                    controller.Attributes.OfType<IEnableCorsAttribute>().Any();

                foreach (var action in controller.Actions)
                {
                    var corsOnAction = action.Attributes.OfType<IDisableCorsAttribute>().Any() ||
                        action.Attributes.OfType<IEnableCorsAttribute>().Any();

                    if (!corsOnController && !corsOnAction)
                    {
                        // No CORS here.
                        continue;
                    }

                    foreach (var selector in action.Selectors)
                    {
                        var metadata = selector.EndpointMetadata;
                        // Read interface .Count once rather than per iteration
                        var metadataCount = metadata.Count;
                        for (var i = 0; i < metadataCount; i++)
                        {
                            if (metadata[i] is HttpMethodMetadata httpMethodMetadata)
                            {
                                metadata[i] = new HttpMethodMetadata(httpMethodMetadata.HttpMethods, acceptCorsPreflight: true);
                            }
                        }
                    }
                }
            }
        }
    }
}
