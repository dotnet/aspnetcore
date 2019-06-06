using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling.Microbenchmarks
{
    public class QueueFullOverhead
    {
        private const int _numRequests = 2000;
        private int _requestCount = 0;
        private TaskCompletionSource<bool> tcs;

        private RequestThrottlingMiddleware _middleware;

        [Params(1,8,64)]
        public int MaxConcurrentRequests;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var options = new RequestThrottlingOptions
            {
                MaxConcurrentRequests = MaxConcurrentRequests,
                RequestQueueLimit = _numRequests
            };

            _middleware = new RequestThrottlingMiddleware(
                    next: (RequestDelegate)_incrementAndCheck,
                    loggerFactory: NullLoggerFactory.Instance,
                    options: Options.Create(options)
                );
        }

        [IterationSetup]
        public void Setup()
        {
            _requestCount = 0;
            tcs = new TaskCompletionSource<bool>();
        }

        private async Task _incrementAndCheck(HttpContext context)
        {
            if (Interlocked.Increment(ref _requestCount) == _numRequests)
            {
                tcs.SetResult(true);
            }

            await Task.Yield();
        }

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public void Baseline()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                _ = _incrementAndCheck(null);
            }

            tcs.Task.GetAwaiter().GetResult();
        }

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public void QueueingAll()
        {
            for (int i = 0; i < _numRequests; i++)
            {
                _ = _middleware.Invoke(null);
            }

            tcs.Task.GetAwaiter().GetResult();
        }
    }
}
