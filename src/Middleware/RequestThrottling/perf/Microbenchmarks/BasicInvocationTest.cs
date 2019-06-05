// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.RequestThrottling.Microbenchmarks
{
    public class BasicInvocationTest
    {
        private RequestThrottlingMiddleware _middleware;

        private RequestDelegate _nextDelay;

        private const int _numRequests = 2000;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _nextDelay = NextDelay;

            var options = new RequestThrottlingOptions
            {
                MaxConcurrentRequests = MaxConcurrentRequests,
                RequestQueueLimit = RequestQueueLimit
            };

            _middleware = new RequestThrottlingMiddleware(
                    next: _nextDelay,
                    loggerFactory: NullLoggerFactory.Instance,
                    options: Options.Create(options)
                );
        }

        [Params(1, 3, 8, 21, 55)]
        public int MaxConcurrentRequests;

        [Params(_numRequests)]
        public int RequestQueueLimit;

        [Benchmark(OperationsPerInvoke = _numRequests)]
        public async Task AwaitEachSequentially()
        {
            // this one should not change with MaxConcurrentRequests
            for (int i = 0; i < _numRequests; i++)
            {
                await _middleware.Invoke(null);
            }
        }

        //[Benchmark(OperationsPerInvoke = _numRequests)]
        //public async Task DumpFirstAwaitSecond()
        //{
        //    for (int i = 0; i < _numRequests - MaxConcurrentRequests; i++)
        //    {
        //        _ = _middleware.Invoke(null);
        //    }

        //    for (int i = 0; i < MaxConcurrentRequests; i++)
        //    {
        //        await _middleware.Invoke(null);
        //    }
        //}

        private async Task NextDelay(HttpContext context)
        {
            for (int i = 0; i < 100000; i++)
            {

            }
            await Task.Yield();
        }
    }

    //public static class Test
    //{
    //    public static void Main()
    //    {
    //        var tasks = new Task[4];

    //        for (int i = 0; i < 4; i++)
    //        {
    //            tasks[i] = Task.Run(TestMethod);
    //            //ThreadPool.UnsafeQueueUserWorkItem<object>(_ => _ = TestMethod(), state: null, preferLocal: false);
    //        }

    //        Task.WaitAll(tasks);

    //        Console.WriteLine("Done!");
    //    }

    //    public static async Task TestMethod()
    //    {
    //        var test = new BasicInvocationTest();

    //        test.GlobalSetup();
    //        test.AwaitEachSequentially().GetAwaiter().GetResult();

    //        await Task.Delay(1000);
            
    //        test.GlobalSetup();
    //        test.AwaitEachSequentially().GetAwaiter().GetResult();

    //    }

    //    public static void RunOnce()
    //    {
    //        var test = new BasicInvocationTest();
    //        test.GlobalSetup();

    //        test.AwaitEachSequentially().GetAwaiter().GetResult();
    //    }
    //}
}
