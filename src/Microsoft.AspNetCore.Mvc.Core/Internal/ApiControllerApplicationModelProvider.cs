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
        /// Order is set to execute after the <see cref="DefaultApplicationModelProvider"/>.
        /// </remarks>
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            foreach (var controllerModel in context.Result.Controllers)
            {
                if (controllerModel.Attributes.OfType<IApiBehaviorMetadata>().Any())
                {
                    if (_apiBehaviorOptions.EnableModelStateInvalidFilter)
                    {
                        Debug.Assert(_apiBehaviorOptions.InvalidModelStateResponseFactory != null);
                        controllerModel.Filters.Add(_modelStateInvalidFilter);
                    }

                    continue;
                }

                foreach (var actionModel in controllerModel.Actions)
                {
                    if (actionModel.Attributes.OfType<IApiBehaviorMetadata>().Any())
                    {
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
