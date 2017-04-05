// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class TempDataApplicationModelProvider : IApplicationModelProvider
    {
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
                var tempDataProperties = SaveTempDataPropertyFilterBase.GetTempDataProperties(modelType);

                if (tempDataProperties != null)
                {
                    var factory = new ControllerSaveTempDataPropertyFilterFactory()
                    {
                        TempDataProperties = tempDataProperties
                    };

                    controllerModel.Filters.Add(factory);
                }
            }
        }
    }
}
