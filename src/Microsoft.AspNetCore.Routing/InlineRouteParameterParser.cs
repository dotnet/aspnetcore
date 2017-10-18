// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Routing.Template;

namespace Microsoft.AspNetCore.Routing
{
    public static class InlineRouteParameterParser
    {
        [Obsolete(
            "This API is obsolete and will be removed in a future release. It does not report errors correctly. " + 
            "Use 'TemplateParser.Parse()' and filter for the desired parameter as an alternative.")]
        public static TemplatePart ParseRouteParameter(string routeParameter)
        {
            if (routeParameter == null)
            {
                throw new ArgumentNullException(nameof(routeParameter));
            }

            // See: #475 - this API has no way to pass the 'raw' text
            var inner = AspNetCore.Dispatcher.Patterns.InlineRouteParameterParser.ParseRouteParameter(string.Empty, routeParameter);
            return new TemplatePart(inner);
        }
    }
}
