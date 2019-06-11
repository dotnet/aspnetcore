// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Threading;

namespace Microsoft.AspNetCore.RequestThrottling.Tests
{
    public static class TestUtils
    {
        private static RequestThrottlingMiddleware CreateTestMiddleware(IRequestQueue queue, RequestDelegate onRejected, RequestDelegate next)
        {
            var options = new RequestThrottlingOptions { OnRejected = onRejected ?? (context => Task.CompletedTask) };

            return new RequestThrottlingMiddleware(
                    next: next ?? (context => Task.CompletedTask),
                    loggerFactory: NullLoggerFactory.Instance,
                    queue: queue,
                    options: options
                );
        }

        internal static IRequestQueue CreateRequestQueue(int maxConcurrentRequests) => new TailDrop(maxConcurrentRequests, 5000);
    }

    class AlwaysBlockStrategy : IRequestQueue
    {
        public async Task<bool> TryEnterQueueAsync()
        {
            // just wait forever; never return
            Thread.Sleep(Timeout.Infinite);
            await Task.CompletedTask;
            return true;
        }

        public void OnExit() { }

        public void Dispose()
        {
            // do the threads really get cleaned up?? let's see
        }
    }
}
