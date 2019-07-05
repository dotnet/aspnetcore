// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Components.Routing
{
    internal readonly struct RouteEntry
    {
        public RouteEntry(RouteTemplate template, Type handler)
        {
            Template = template;
            Handler = handler;
        }

        public RouteTemplate Template { get; }

        public Type Handler { get; }

        internal void Match(RouteContext context)
        {
            if (Template.Segments.Length != context.Segments.Length)
            {
                return;
            }

            // Parameters will be lazily initialized.
            Dictionary<string, object> parameters = null;
            for (var i = 0; i < Template.Segments.Length; i++)
            {
                var segment = Template.Segments[i];
                var pathSegment = context.Segments[i];
                if (!segment.Match(pathSegment, out var matchedParameterValue))
                {
                    return;
                }
                else
                {
                    if (segment.IsParameter)
                    {
                        parameters ??= new Dictionary<string, object>(StringComparer.Ordinal);
                        parameters[segment.Value] = matchedParameterValue;
                    }
                }
            }

            context.Parameters = parameters;
            context.Handler = Handler;
        }
    }
}
