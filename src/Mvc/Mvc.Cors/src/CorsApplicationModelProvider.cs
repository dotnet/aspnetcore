// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.Cors
{
    internal class CorsApplicationModelProvider : IApplicationModelProvider
    {
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
                        UpdateActionToAcceptCorsPreflight(actionModel);
                    }
                }
            }
        }

        private static void UpdateActionToAcceptCorsPreflight(ActionModel actionModel)
        {
            for (var i = 0; i < actionModel.Selectors.Count; i++)
            {
                var selectorModel = actionModel.Selectors[i];

                for (var j = 0; j < selectorModel.ActionConstraints.Count; j++)
                {
                    if (selectorModel.ActionConstraints[j] is HttpMethodActionConstraint httpConstraint)
                    {
                        selectorModel.ActionConstraints[j] = new CorsHttpMethodActionConstraint(httpConstraint);
                    }
                }

                for (int j = 0; j < selectorModel.EndpointMetadata.Count; j++)
                {
                    if (selectorModel.EndpointMetadata[j] is HttpMethodMetadata httpMethodMetadata)
                    {
                        selectorModel.EndpointMetadata[j] = new HttpMethodMetadata(httpMethodMetadata.HttpMethods, true);
                    }
                }
            }
        }
    }
}
