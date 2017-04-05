// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Internal
{
    public class PageSaveTempDataPropertyFilterFactory : IFilterFactory
    {
        public IList<TempDataProperty> TempDataProperties { get; set; }

        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var service = serviceProvider.GetRequiredService<PageSaveTempDataPropertyFilter>();
            service.FilterFactory = this;

            return service;
        }

        public IList<TempDataProperty> GetTempDataProperties(Type modelType)
        {
            // TempDataProperties are stored here as a cache for the filter. But in pages by the time we know the type
            // of our model we no longer have access to the factory, so we store the factory on the filter so it can
            // call this method to populate its TempDataProperties.
            if (TempDataProperties == null)
            {
                TempDataProperties = SaveTempDataPropertyFilterBase.GetTempDataProperties(modelType);
            }

            return TempDataProperties;
        }
    }
}