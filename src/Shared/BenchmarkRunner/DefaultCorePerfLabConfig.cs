// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    internal class DefaultCorePerfLabConfig : ManualConfig
    {
        public DefaultCorePerfLabConfig()
        {
            Add(ConsoleLogger.Default);

            Add(MemoryDiagnoser.Default);
            Add(StatisticColumn.OperationsPerSecond);
            Add(new ParamsSummaryColumn());
            Add(DefaultColumnProviders.Statistics, DefaultColumnProviders.Diagnosers, DefaultColumnProviders.Target);

            // TODO: When upgrading to BDN 0.11.1, use Add(DefaultColumnProviders.Descriptor); 
            // DefaultColumnProviders.Target is deprecated

            Add(JitOptimizationsValidator.FailOnError);

            Add(Job.InProcess
                .With(RunStrategy.Throughput));

            Add(MarkdownExporter.GitHub);

            Add(new CsvExporter(
                CsvSeparator.Comma,
                new Reports.SummaryStyle
                {
                    PrintUnitsInHeader = true,
                    PrintUnitsInContent = false,
                    TimeUnit = Horology.TimeUnit.Microsecond,
                    SizeUnit = SizeUnit.KB
                }));
        }
    }
}
