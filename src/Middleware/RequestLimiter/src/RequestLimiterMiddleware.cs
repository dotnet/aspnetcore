// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.RateLimits;
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

        public Task Invoke(HttpContext context)
        {
            _logger.LogInformation("Resource limiting: " + context.Request.Path);

            var endpoint = context.GetEndpoint();
            var attributes = endpoint?.Metadata.GetOrderedMetadata<RequestLimitAttribute>();
            var noLimitAttributes = endpoint?.Metadata.GetOrderedMetadata<NoRequestLimitAttribute>();

            if (attributes == null || noLimitAttributes != null)
            {
                return _next.Invoke(context);
            }

            return InvokeAsync(context, attributes);
        }
        private async Task InvokeAsync(HttpContext context, IReadOnlyList<RequestLimitAttribute> attributes)
        {
            var permitLeases = new Stack<PermitLease>();
            try
            {
                foreach (var attribute in attributes)
                {
                    // At most one of Policy or Limiter can be set.
                    Debug.Assert(string.IsNullOrEmpty(attribute.Policy) || attribute.Limiter == null);

                    if (string.IsNullOrEmpty(attribute.Policy) && attribute.Limiter == null)
                    {
                        if (_options.ResolveDefaultRequestLimit != null)
                        {
                            if (!await ApplyLimitAsync(_options.ResolveDefaultRequestLimit(context.RequestServices), context, permitLeases))
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
                            if (!await ApplyLimitAsync(limitResolver(context.RequestServices), context, permitLeases))
                            {
                                return;
                            }
                        }
                    }

                    if (attribute.Limiter != null)
                    {
                        // Registrations based limiters
                        if (!await ApplyLimitAsync(attribute.Limiter, context, permitLeases))
                        {
                            return;
                        }
                    }

                }

                await _next.Invoke(context);
            }
            finally
            {
                while (permitLeases.TryPop(out var resource))
                {
                    _logger.LogInformation("Releasing resource");
                    resource.Dispose();
                }
            };
        }
        private Task<bool> ApplyLimitAsync(AggregatedRateLimiter<HttpContext> limiter, HttpContext context, Stack<PermitLease> obtainedResources)
        {
            _logger.LogInformation("Resource count: " + limiter.AvailablePermits(context));
            var resourceLeaseTask = limiter.WaitAsync(context);

            if (resourceLeaseTask.IsCompletedSuccessfully)
            {
                var resourceLease = resourceLeaseTask.Result;
                if (!resourceLease.IsAcquired)
                {
                    _logger.LogInformation("Resource exhausted");
                    context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                    return OnRejectAsync(context, resourceLease);
                }

                _logger.LogInformation("Resource obtained");
                obtainedResources.Push(resourceLease);
                return Task.FromResult(true);
            }

            return ApplyLimitAsyncAwaited(resourceLeaseTask, context, obtainedResources);
        }

        private async Task<bool> OnRejectAsync(HttpContext context, PermitLease resourceLease)
        {
            await _options.OnRejected(context, resourceLease);
            return false;
        }

        private async Task<bool> ApplyLimitAsyncAwaited(ValueTask<PermitLease> resourceLeaseTask, HttpContext context, Stack<PermitLease> obtainedResources)
        {
            var resourceLease = await resourceLeaseTask;
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
