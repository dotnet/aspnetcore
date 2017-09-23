// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class ApiControllerApplicationModelProvider : IApplicationModelProvider
    {
        private readonly ApiBehaviorOptions _apiBehaviorOptions;
        private readonly ModelStateInvalidFilter _modelStateInvalidFilter;

        public ApiControllerApplicationModelProvider(IOptions<ApiBehaviorOptions> apiBehaviorOptions, ILoggerFactory loggerFactory)
        {
            _apiBehaviorOptions = apiBehaviorOptions.Value;
            if (_apiBehaviorOptions.EnableModelStateInvalidFilter && _apiBehaviorOptions.InvalidModelStateResponseFactory == null)
            {
                throw new ArgumentException(Resources.FormatPropertyOfTypeCannotBeNull(
                    typeof(ApiBehaviorOptions),
                    nameof(ApiBehaviorOptions.InvalidModelStateResponseFactory)));
            }

            _modelStateInvalidFilter = new ModelStateInvalidFilter(
                apiBehaviorOptions.Value,
                loggerFactory.CreateLogger<ModelStateInvalidFilter>());
        }

        /// <remarks>
        /// Order is set to execute after the <see cref="DefaultApplicationModelProvider"/> and allow any other user
        /// <see cref="IApplicationModelProvider"/> that configure routing to execute.
        /// </remarks>
        public int Order => -1000 + 100;

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            foreach (var controllerModel in context.Result.Controllers)
            {
                var isApiController = controllerModel.Attributes.OfType<IApiBehaviorMetadata>().Any();
                var controllerHasSelectorModel = controllerModel.Selectors.Any(s => s.AttributeRouteModel != null);

                foreach (var actionModel in controllerModel.Actions)
                {
                    if (isApiController || actionModel.Attributes.OfType<IApiBehaviorMetadata>().Any())
                    {
                        if (!controllerHasSelectorModel && !actionModel.Selectors.Any(s => s.AttributeRouteModel != null))
                        {
                            // Require attribute routing with controllers annotated with ApiControllerAttribute
                            throw new InvalidOperationException(Resources.FormatApiController_AttributeRouteRequired(nameof(ApiControllerAttribute)));
                        }

                        if (_apiBehaviorOptions.EnableModelStateInvalidFilter)
                        {
                            Debug.Assert(_apiBehaviorOptions.InvalidModelStateResponseFactory != null);
                            actionModel.Filters.Add(_modelStateInvalidFilter);
                        }
                    }
                }
            }
        }
    }
}
