// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Routing
{
    internal class RouteEntry
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
            IDictionary<string, string> parameters = null;
            for (int i = 0; i < Template.Segments.Length; i++)
            {
                var segment = Template.Segments[i];
                var pathSegment = context.Segments[i];
                if (!segment.Match(pathSegment))
                {
                    return;
                }
                else
                {
                    if (segment.IsParameter)
                    {
                        GetParameters()[segment.Value] = pathSegment;
                    }
                }
            }

            context.Parameters = parameters;
            context.Handler = Handler;

            IDictionary<string, string> GetParameters()
            {
                if (parameters == null)
                {
                    parameters = new Dictionary<string, string>();
                }

                return parameters;
            }
        }
    }
}
