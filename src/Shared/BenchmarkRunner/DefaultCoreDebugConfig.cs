// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes;

internal sealed class DefaultCoreDebugConfig : ManualConfig
{
    public DefaultCoreDebugConfig()
    {
        AddLogger(ConsoleLogger.Default);
        AddValidator(JitOptimizationsValidator.DontFailOnError);

        AddJob(Job.InProcess
            .WithStrategy(RunStrategy.Throughput));
    }
}
