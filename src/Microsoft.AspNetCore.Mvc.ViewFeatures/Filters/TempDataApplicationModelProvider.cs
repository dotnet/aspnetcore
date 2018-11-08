// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal class TempDataApplicationModelProvider : IApplicationModelProvider
    {
        private readonly MvcViewOptions _options;

        public TempDataApplicationModelProvider(IOptions<MvcViewOptions> options)
        {
            _options = options.Value;
        }

        /// <inheritdoc />
        /// <remarks>This order ensures that <see cref="TempDataApplicationModelProvider"/> runs after the <see cref="DefaultApplicationModelProvider"/>.</remarks>
        public int Order => -1000 + 10;

        /// <inheritdoc />
        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
        }

        /// <inheritdoc />
        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            foreach (var controllerModel in context.Result.Controllers)
            {
                var modelType = controllerModel.ControllerType.AsType();

                var tempDataProperties = SaveTempDataPropertyFilterBase.GetTempDataProperties(modelType, _options);
                if (tempDataProperties == null)
                {
                    continue;
                }

                var filter = new ControllerSaveTempDataPropertyFilterFactory(tempDataProperties);
                controllerModel.Filters.Add(filter);
            }
        }
    }
}
