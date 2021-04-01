// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal class ViewDataAttributeApplicationModelProvider : IApplicationModelProvider
    {
        /// <inheritdoc />
        /// <remarks>This order ensures that <see cref="ViewDataAttributeApplicationModelProvider"/> runs after the <see cref="DefaultApplicationModelProvider"/>.</remarks>
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
                var controllerType = controllerModel.ControllerType.AsType();

                var viewDataProperties = ViewDataAttributePropertyProvider.GetViewDataProperties(controllerType);
                if (viewDataProperties == null)
                {
                    continue;
                }

                var filter = new ControllerViewDataAttributeFilterFactory(viewDataProperties);
                controllerModel.Filters.Add(filter);
            }
        }
    }
}
