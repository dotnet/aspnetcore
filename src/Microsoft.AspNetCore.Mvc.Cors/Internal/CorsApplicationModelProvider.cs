// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.Cors.Internal
{
    public class CorsApplicationModelProvider : IApplicationModelProvider
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
                }
            }
        }
    }
}
