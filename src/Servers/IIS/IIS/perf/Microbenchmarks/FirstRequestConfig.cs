// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    internal class FirstRequestConfig : ManualConfig
    {
        public FirstRequestConfig()
        {
            AddLogger(ConsoleLogger.Default);
            AddExporter(MarkdownExporter.GitHub);

            AddDiagnoser(MemoryDiagnoser.Default);
            AddColumn(StatisticColumn.OperationsPerSecond);
            AddColumnProvider(DefaultColumnProviders.Instance);

            AddValidator(JitOptimizationsValidator.FailOnError);

            AddJob(Job.Default
                .WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21))
                .WithGcMode(new GcMode { Server = true })
                .WithIterationCount(10)
                .WithInvocationCount(1)
                .WithUnrollFactor(1)
                .WithStrategy(RunStrategy.ColdStart));
        }
    }
}
