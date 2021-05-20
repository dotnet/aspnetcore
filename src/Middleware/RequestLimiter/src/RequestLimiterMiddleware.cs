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
                // TODO: Apply default policy
                await _next.Invoke(context);
                return;
            }

            var resources = new Stack<Resource>();
            try
            {
                foreach (var attribute in attributes)
                {
                    if (!string.IsNullOrEmpty(attribute.Policy) && attribute.LimiterRegistration != null)
                    {
                        throw new InvalidOperationException("Cannot specify both policy and limiter registration");
                    }

                    if (string.IsNullOrEmpty(attribute.Policy) || attribute.LimiterRegistration == null)
                    {
                        throw new InvalidOperationException("No policy or registration on attribute");
                    }

                    // Policy based limiters
                    if (!string.IsNullOrEmpty(attribute.Policy))
                    {
                        if (!_options.PolicyMap.TryGetValue(attribute.Policy, out var policy))
                        {
                            throw new InvalidOperationException("Policy not found");
                        }

                        foreach (var registration in policy.Limiters)
                        {
                            if (!ApplyLimit(registration, context, resources))
                            {
                                return;
                            }
                        }
                    }

                    // Registrations based limiters
                    if (!ApplyLimit(attribute.LimiterRegistration, context, resources))
                    {
                        return;
                    }
                }

                await _next.Invoke(context);
            }
            finally
            {
                while (resources.TryPop(out var resource))
                {
                    _logger.LogInformation("Releasing resource");
                    resource.Dispose();
                }
            };
        }

        private bool ApplyLimit(RequestLimitRegistration registration, HttpContext context, Stack<Resource> obtainedResources)
        {
            if (registration.ResolveLimiter != null)
            {
                var limiter = registration.ResolveLimiter(context.RequestServices);
                _logger.LogInformation("Resource count: " + limiter.EstimatedCount);
                if (!limiter.TryAcquire(out var resource))
                {
                    _logger.LogInformation("Resource exhausted");
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return false;
                }

                _logger.LogInformation("Resource obtained");
                obtainedResources.Push(resource.Value);
                return true;
            }
            if (registration.ResolveAggregatedLimiter != null)
            {
                var limiter = registration.ResolveAggregatedLimiter(context.RequestServices);
                _logger.LogInformation("Resource count: " + limiter.EstimatedCount(context));
                if (!limiter.TryAcquire(context, out var resource))
                {
                    _logger.LogInformation("Resource exhausted");
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return false;
                }

                _logger.LogInformation("Resource obtained");
                obtainedResources.Push(resource.Value);
                return true;
            }
            throw new InvalidOperationException("Registration couldn't resolve limiter");
        }
    }
}
