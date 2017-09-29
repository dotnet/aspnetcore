// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Dispatcher;

namespace DispatcherSample
{
    public class HttpMethodEndpointSelector : EndpointSelector
    {
        public override async Task SelectAsync(EndpointSelectorContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var snapshot = context.CreateSnapshot();

            var fallback = new List<Endpoint>();
            for (var i = context.Endpoints.Count - 1; i >= 0; i--)
            {
                var endpoint = context.Endpoints[i] as ITemplateEndpoint;
                if (endpoint == null || endpoint.HttpMethod == null)
                {
                    // No metadata.
                    fallback.Add(context.Endpoints[i]);
                    context.Endpoints.RemoveAt(i);
                }
                else if (string.Equals(endpoint.HttpMethod, context.HttpContext.Request.Method, StringComparison.OrdinalIgnoreCase))
                {
                    // This one matches.
                }
                else
                {
                    // Not a match.
                    context.Endpoints.RemoveAt(i);
                }
            }

            // Now the list of endpoints only contains those that have an HTTP method preference AND match the current
            // request.
            await context.InvokeNextAsync();

            if (context.Endpoints.Count == 0)
            {
                // Nothing matched, do the fallback.
                context.RestoreSnapshot(snapshot);
                context.Endpoints.Clear();

                for (var i = 0; i < fallback.Count; i++)
                {
                    context.Endpoints.Add(fallback[i]);
                }

                await context.InvokeNextAsync();
            }
        }
    }
}
