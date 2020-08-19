// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.SignalR.Internal;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class TypedClientBuilderBenchmark
    {
        private static readonly IClientProxy Dummy = new DummyProxy();

        [Benchmark]
        public ITestClient Build()
        {
            return TypedClientBuilder<ITestClient>.Build(Dummy);
        }

        public interface ITestClient { }

        private class DummyProxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object[] args, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}
