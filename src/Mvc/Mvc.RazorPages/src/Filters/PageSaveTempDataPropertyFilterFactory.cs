// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class PageSaveTempDataPropertyFilterFactory : IFilterFactory
    {
        public PageSaveTempDataPropertyFilterFactory(IReadOnlyList<LifecycleProperty> properties)
        {
            Properties = properties;
        }

        public IReadOnlyList<LifecycleProperty> Properties { get; }

        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            var service = serviceProvider.GetRequiredService<PageSaveTempDataPropertyFilter>();
            service.Properties = Properties;

            return service;
        }
    }
}