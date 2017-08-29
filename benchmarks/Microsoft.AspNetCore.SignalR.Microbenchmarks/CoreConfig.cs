using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Validators;

namespace Microsoft.AspNetCore.SignalR.Microbenchmarks
{
    public class CoreConfig : ManualConfig
    {
        public CoreConfig()
        {
            Add(JitOptimizationsValidator.FailOnError);
            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.OperationsPerSecond);

            Add(Job.Default
                .With(BenchmarkDotNet.Environments.Runtime.Core)
                .WithRemoveOutliers(false)
                .With(new GcMode() { Server = true })
                .With(RunStrategy.Throughput)
                .WithLaunchCount(3)
                .WithWarmupCount(5)
                .WithTargetCount(10));
        }
    }
}
