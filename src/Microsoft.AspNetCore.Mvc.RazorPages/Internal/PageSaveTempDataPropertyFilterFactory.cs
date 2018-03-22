// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages.Internal;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.RazorPages
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