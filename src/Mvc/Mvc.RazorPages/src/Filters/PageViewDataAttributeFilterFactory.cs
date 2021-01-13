// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Filters;

namespace Microsoft.AspNetCore.Mvc.Filters
{
    internal class PageViewDataAttributeFilterFactory : IFilterFactory
    {
        public PageViewDataAttributeFilterFactory(IReadOnlyList<LifecycleProperty> properties)
        {
            Properties = properties;
        }

        public IReadOnlyList<LifecycleProperty> Properties { get; }

        // PageViewDataAttributeFilter is stateful and cannot be reused.
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new PageViewDataAttributeFilter(Properties);
        }
    }
}
