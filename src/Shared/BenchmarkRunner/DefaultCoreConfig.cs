// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

namespace BenchmarkDotNet.Attributes;

internal sealed class DefaultCoreConfig : ManualConfig
{
    public DefaultCoreConfig()
    {
        AddLogger(ConsoleLogger.Default);
        AddExporter(MarkdownExporter.GitHub);

        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(StatisticColumn.OperationsPerSecond);
        AddColumnProvider(DefaultColumnProviders.Instance);

        AddValidator(JitOptimizationsValidator.FailOnError);

        AddJob(Job.Default
#if NETCOREAPP2_1
                .WithToolchain(CsProjCoreToolchain.From(NetCoreAppSettings.NetCoreApp21))
#elif NETCOREAPP3_0
                .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings("netcoreapp3.0", null, ".NET Core 3.0")))
#elif NETCOREAPP3_1
                .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings("netcoreapp3.1", null, ".NET Core 3.1")))
#elif NET5_0
                .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings("net5.0", null, ".NET Core 5.0")))
#elif NET6_0
                .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings("net6.0", null, ".NET Core 6.0")))
#elif NET7_0
                .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings("net7.0", null, ".NET Core 7.0")))
#elif NET8_0
                .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings("net8.0", null, ".NET Core 8.0")))
#elif NET9_0
                .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings("net9.0", null, ".NET Core 9.0")))
#else
#error Target frameworks need to be updated.
#endif
                .WithGcMode(new GcMode { Server = true })
            .WithStrategy(RunStrategy.Throughput));
    }
}
