// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.ResourceLimits;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestLimiter
{
    public class RequestLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly RequestLimiterOptions _options;

        public RequestLimiterMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, IOptions<RequestLimiterOptions> options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RequestLimiterMiddleware>();
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("Resource limiting: " + context.Request.Path);

            var endpoint = context.GetEndpoint();
            var attributes = endpoint?.Metadata.GetOrderedMetadata<RequestLimitAttribute>();

            if (attributes == null)
            {
                await _next.Invoke(context);
                return;
            }

            var resourceLeases = new Stack<ResourceLease>();
            try
            {
                foreach (var attribute in attributes)
                {
                    if (!string.IsNullOrEmpty(attribute.Policy) && attribute.Limiter != null)
                    {
                        throw new InvalidOperationException("Cannot specify both policy and limiter");
                    }

                    if (string.IsNullOrEmpty(attribute.Policy) && attribute.Limiter == null)
                    {
                        if (_options.ResolveDefaultRequestLimit != null)
                        {
                            if (!await ApplyLimitAsync(_options.ResolveDefaultRequestLimit(context.RequestServices), context, resourceLeases))
                            {
                                return;
                            }
                        }
                    }

                    // Policy based limiters
                    if (!string.IsNullOrEmpty(attribute.Policy))
                    {
                        if (!_options.PolicyMap.TryGetValue(attribute.Policy, out var policy))
                        {
                            throw new InvalidOperationException("Policy not found");
                        }

                        foreach (var limitResolver in policy.LimiterResolvers)
                        {
                            if (!await ApplyLimitAsync(limitResolver(context.RequestServices), context, resourceLeases))
                            {
                                return;
                            }
                        }
                    }

                    if (attribute.Limiter != null)
                    {
                        // Registrations based limiters
                        if (!await ApplyLimitAsync(attribute.Limiter, context, resourceLeases))
                        {
                            return;
                        }
                    }

                }

                await _next.Invoke(context);
            }
            finally
            {
                while (resourceLeases.TryPop(out var resource))
                {
                    _logger.LogInformation("Releasing resource");
                    resource.Dispose();
                }
            };
        }

        private async Task<bool> ApplyLimitAsync(AggregatedResourceLimiter<HttpContext> limiter, HttpContext context, Stack<ResourceLease> obtainedResources)
        {
            _logger.LogInformation("Resource count: " + limiter.EstimatedCount(context));
            var resourceLease = await limiter.WaitAsync(context);
            if (!resourceLease.IsAcquired)
            {
                _logger.LogInformation("Resource exhausted");
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await _options.OnRejected(context, resourceLease);
                return false;
            }

            _logger.LogInformation("Resource obtained");
            obtainedResources.Push(resourceLease);
            return true;
        }
    }
}
