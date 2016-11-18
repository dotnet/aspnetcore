// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class DefaultConfig : ManualConfig
    {
        public DefaultConfig()
        {
            Add(JitOptimizationsValidator.FailOnError);
            Add(new RpsColumn());

            Add(Job.Default.
                With(Platform.X64).
                With(Jit.RyuJit).
                With(BenchmarkDotNet.Environments.Runtime.Clr).
                With(new GcMode() { Server = true }).
                With(RunStrategy.Throughput).
                WithLaunchCount(3).
                WithWarmupCount(5).
                WithTargetCount(10));

            Add(Job.Default.
                With(BenchmarkDotNet.Environments.Runtime.Core).
                With(new GcMode() { Server = true }).
                With(RunStrategy.Throughput).
                WithLaunchCount(3).
                WithWarmupCount(5).
                WithTargetCount(10));
        }
    }
}
