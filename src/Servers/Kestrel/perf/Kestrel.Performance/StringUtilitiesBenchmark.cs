// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class StringUtilitiesBenchmark
    {
        private const int Iterations = 500_000;

        [Benchmark(Baseline = true, OperationsPerInvoke = Iterations)]
        public void UintToString()
        {
            var connectionId = CorrelationIdGenerator.GetNextId();
            for (uint i = 0; i < Iterations; i++)
            {
                var id = connectionId + ':' + i.ToString("X8");
            }
        }

        [Benchmark(OperationsPerInvoke = Iterations)]
        public void ConcatAsHexSuffix()
        {
            var connectionId = CorrelationIdGenerator.GetNextId();
            for (uint i = 0; i < Iterations; i++)
            {
                var id = StringUtilities.ConcatAsHexSuffix(connectionId, ':', i);
            }
        }
    }
}
