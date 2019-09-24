// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Filters
{
    internal class ControllerViewDataAttributeFilterFactory : IFilterFactory
    {
        public ControllerViewDataAttributeFilterFactory(IReadOnlyList<LifecycleProperty> properties)
        {
            Properties = properties;
        }

        public IReadOnlyList<LifecycleProperty> Properties { get; }

        // ControllerViewDataAttributeFilter is stateful and cannot be reused.
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new ControllerViewDataAttributeFilter(Properties);
        }
    }
}
