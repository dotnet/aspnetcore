// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal class ViewDataAttributePageApplicationModelProvider : IPageApplicationModelProvider
    {
        /// <inheritdoc />
        /// <remarks>This order ensures that <see cref="ViewDataAttributePageApplicationModelProvider"/> runs after the <see cref="DefaultPageApplicationModelProvider"/>.</remarks>
        public int Order => -1000 + 10;

        /// <inheritdoc />
        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }

        /// <inheritdoc />
        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var handlerType = context.PageApplicationModel.HandlerType.AsType();

            var viewDataProperties = ViewDataAttributePropertyProvider.GetViewDataProperties(handlerType);
            if (viewDataProperties == null)
            {
                return;
            }

            var filter = new PageViewDataAttributeFilterFactory(viewDataProperties);
            context.PageApplicationModel.Filters.Add(filter);
        }
    }
}
