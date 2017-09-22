// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Dispatcher;

namespace Microsoft.AspNetCore.Routing.Dispatcher
{
    public class RouteTemplateMetadata : IRouteTemplateMetadata
    {
        public RouteTemplateMetadata(string routeTemplate)
            : this(routeTemplate, null)
        {
        }

        public RouteTemplateMetadata(string routeTemplate, object defaults)
        {
            if (routeTemplate == null)
            {
                throw new ArgumentNullException(nameof(routeTemplate));
            }

            RouteTemplate = routeTemplate;
            Defaults = new DispatcherValueCollection(defaults);
        }

        public string RouteTemplate { get; }

        public DispatcherValueCollection Defaults { get; }
    }
}
