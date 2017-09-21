// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherValueEndpointSelector : EndpointSelector
    {
        public override Task SelectAsync(EndpointSelectorContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var dispatcherFeature = context.HttpContext.Features.Get<IDispatcherFeature>();

            for (var i = context.Endpoints.Count - 1; i >= 0; i--)
            {
                var endpoint = context.Endpoints[i] as IDispatcherValueSelectableEndpoint;
                if (!CompareRouteValues(dispatcherFeature.Values, endpoint.Values))
                {
                    context.Endpoints.RemoveAt(i);
                }
            }
            
            return context.InvokeNextAsync();
        }
        
        private bool CompareRouteValues(DispatcherValueCollection values, DispatcherValueCollection requiredValues)
        {
            foreach (var kvp in requiredValues)
            {
                if (string.IsNullOrEmpty(kvp.Value.ToString()))
                {
                    if (values.TryGetValue(kvp.Key, out var routeValue) && !string.IsNullOrEmpty(routeValue.ToString()))
                    {
                        return false;
                    }
                }
                else
                {
                    if (!values.TryGetValue(kvp.Key, out var routeValue) || !string.Equals(kvp.Value.ToString(), routeValue.ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
