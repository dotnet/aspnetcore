// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.RequestThrottling.Internal;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public static class TestUtils
    {
        public static RequestThrottlingMiddleware CreateTestMiddleware(int? maxConcurrentRequests, int requestQueueLimit = 5000, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            var options = new RequestThrottlingOptions
            {
                MaxConcurrentRequests = maxConcurrentRequests,
                RequestQueueLimit = requestQueueLimit
            };

            return BuildFromOptions(options, onRejected, next);
        }

        public static RequestThrottlingMiddleware CreateBlockingTestMiddleware(int requestQueueLimit = 5000, RequestDelegate onRejected = null, RequestDelegate next = null)
        {
            var options = new RequestThrottlingOptions
            {
                MaxConcurrentRequests = 999,
                RequestQueueLimit = requestQueueLimit,
                ServerAlwaysBlocks = true
            };

            return BuildFromOptions(options, onRejected, next);
        }

        private static RequestThrottlingMiddleware BuildFromOptions(RequestThrottlingOptions options, RequestDelegate onRejected, RequestDelegate next)
        {
            if (onRejected != null)
            {
                options.OnRejected = onRejected;
            }

            return new RequestThrottlingMiddleware(
                    next: next ?? (context => Task.CompletedTask),
                    loggerFactory: NullLoggerFactory.Instance,
                    options: Options.Create(options)
                );
        }

        internal static IRequestQueue CreateRequestQueue(int maxConcurrentRequests) => new TailDrop(maxConcurrentRequests, 5000);
    }
}
