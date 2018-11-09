// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.ApplicationModels
{
    internal class TempDataFilterPageApplicationModelProvider : IPageApplicationModelProvider
    {
        private readonly MvcViewOptions _options;

        public TempDataFilterPageApplicationModelProvider(IOptions<MvcViewOptions> options)
        {
            _options = options.Value;
        }

        // The order is set to execute after the DefaultPageApplicationModelProvider.
        public int Order => -1000 + 10;

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var pageApplicationModel = context.PageApplicationModel;
            var handlerType = pageApplicationModel.HandlerType.AsType();

            var tempDataProperties = SaveTempDataPropertyFilterBase.GetTempDataProperties(handlerType, _options);
            if (tempDataProperties == null)
            {
                return;
            }

            var filter = new PageSaveTempDataPropertyFilterFactory(tempDataProperties);
            pageApplicationModel.Filters.Add(filter);
        }
    }
}
