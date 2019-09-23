// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    internal class DefaultCoreProfileConfig : ManualConfig
    {
        public DefaultCoreProfileConfig()
        {
            Add(ConsoleLogger.Default);
            Add(MarkdownExporter.GitHub);

            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.OperationsPerSecond);
            Add(DefaultColumnProviders.Instance);

            Add(JitOptimizationsValidator.FailOnError);

            Add(Job.InProcess
                .With(RunStrategy.Throughput));
        }
    }
}
