// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal class ControllerSaveTempDataPropertyFilterFactory : IFilterFactory
    {
        public ControllerSaveTempDataPropertyFilterFactory(IReadOnlyList<LifecycleProperty> properties)
        {
            TempDataProperties = properties;
        }

        public IReadOnlyList<LifecycleProperty> TempDataProperties { get; }

        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var service = serviceProvider.GetRequiredService<ControllerSaveTempDataPropertyFilter>();
            service.Properties = TempDataProperties;
            return service;
        }
    }
}
