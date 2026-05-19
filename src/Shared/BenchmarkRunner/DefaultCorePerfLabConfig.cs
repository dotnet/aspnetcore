// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes;

internal sealed class DefaultCorePerfLabConfig : ManualConfig
{
    public DefaultCorePerfLabConfig()
    {
        AddLogger(ConsoleLogger.Default);

        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.OperationsPerSecond);
        AddColumn(new ParamsSummaryColumn());
        AddColumnProvider(DefaultColumnProviders.Statistics, DefaultColumnProviders.Metrics, DefaultColumnProviders.Descriptor);

        AddValidator(JitOptimizationsValidator.FailOnError);

        AddJob(Job.InProcess
            .WithStrategy(RunStrategy.Throughput));

        AddExporter(MarkdownExporter.GitHub);

        AddExporter(new CsvExporter(
            CsvSeparator.Comma,
            new Reports.SummaryStyle(cultureInfo: null, printUnitsInHeader: true, printUnitsInContent: false, timeUnit: Perfolizer.Horology.TimeUnit.Microsecond, sizeUnit: SizeUnit.KB)));
    }
}
