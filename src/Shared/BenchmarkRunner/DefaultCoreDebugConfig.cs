// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace BenchmarkDotNet.Attributes
{
    internal class DefaultCoreDebugConfig : ManualConfig
    {
        public DefaultCoreDebugConfig()
        {
            Add(ConsoleLogger.Default);
            Add(JitOptimizationsValidator.DontFailOnError);

            Add(Job.InProcess
                .With(RunStrategy.Throughput));
        }
    }
}
