// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing
{
    public static class InlineRouteParameterParser
    {
        public static TemplatePart ParseRouteParameter(string routeParameter)
        {
            if (routeParameter == null)
            {
                throw new ArgumentNullException(nameof(routeParameter));
            }

            var inner = AspNetCore.Dispatcher.InlineRouteParameterParser.ParseRouteParameter(routeParameter);
            return new TemplatePart(inner);
        }
    }
}
