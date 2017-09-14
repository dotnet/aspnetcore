// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Dispatcher
{
    public class DispatcherMiddleware
    {
        private readonly DispatcherOptions _options;
        private readonly RequestDelegate _next;

        public DispatcherMiddleware(IOptions<DispatcherOptions> options, RequestDelegate next)
        {
            _options = options.Value;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            foreach (var entry in _options.DispatcherEntryList)
            {
                var parsedTemplate = entry.RouteTemplate;
                var defaults = GetDefaults(parsedTemplate);
                var templateMatcher = new TemplateMatcher(parsedTemplate, defaults);
                var values = new RouteValueDictionary();

                foreach (var endpoint in entry.Endpoints)
                {
                    if (templateMatcher.TryMatch(httpContext.Request.Path, values))
                    {
                        if (!CompareRouteValues(values, endpoint.RequiredValues))
                        {
                            values.Clear();
                        }

                        else
                        {
                            var dispatcherFeature = new DispatcherFeature
                            {
                                Endpoint = endpoint,
                                RequestDelegate = endpoint.RequestDelegate
                            };

                            httpContext.Features.Set<IDispatcherFeature>(dispatcherFeature);
                            break;
                        }
                    }
                }
            }

            await _next(httpContext);
        }

        private RouteValueDictionary GetDefaults(RouteTemplate parsedTemplate)
        {
            var result = new RouteValueDictionary();

            foreach (var parameter in parsedTemplate.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    result.Add(parameter.Name, parameter.DefaultValue);
                }
            }

            return result;
        }

        private bool CompareRouteValues(RouteValueDictionary values, RouteValueDictionary requiredValues)
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
