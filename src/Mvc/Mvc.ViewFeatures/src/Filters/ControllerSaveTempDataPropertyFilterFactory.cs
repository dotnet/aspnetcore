// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
