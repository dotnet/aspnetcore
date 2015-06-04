// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Cors.Core;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    public class CorsApplicationModelProvider : IApplicationModelProvider
    {
        public int Order {  get { return DefaultOrder.DefaultFrameworkSortOrder + 10; } }

        public void OnProvidersExecuted([NotNull]ApplicationModelProviderContext context)
        {
            // Intentionally empty.
        }

        public void OnProvidersExecuting([NotNull]ApplicationModelProviderContext context)
        {
            IEnableCorsAttribute enableCors;
            IDisableCorsAttribute disableCors;

            foreach (var controllerModel in context.Result.Controllers)
            {
                enableCors = controllerModel.Attributes.OfType<IEnableCorsAttribute>().FirstOrDefault();
                if (enableCors != null)
                {
                    controllerModel.Filters.Add(new CorsAuthorizationFilterFactory(enableCors.PolicyName));
                }

                disableCors = controllerModel.Attributes.OfType<IDisableCorsAttribute>().FirstOrDefault();
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
