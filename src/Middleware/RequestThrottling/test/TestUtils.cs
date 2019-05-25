// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.RequestThrottling;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.RequestThrottling.Internal;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public static class TestUtils
    {
        public static RequestThrottlingMiddleware CreateTestMiddleWare(int? maxConcurrentRequests, RequestDelegate next = null)
        {
            var options = new RequestThrottlingOptions
            {
                MaxConcurrentRequests = maxConcurrentRequests
            };

            return new RequestThrottlingMiddleware(
                    next: next ?? (context => Task.CompletedTask),
                    loggerFactory: NullLoggerFactory.Instance,
                    options: Options.Create(options)
                );
        }
    }
}
