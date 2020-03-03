// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Toolchains.InProcess.NoEmit;

namespace BenchmarkDotNet.Attributes
{
    internal class DefaultCoreValidationConfig : ManualConfig
    {
        public DefaultCoreValidationConfig()
        {
            Add(ConsoleLogger.Default);

            Add(Job.Dry.With(InProcessNoEmitToolchain.Instance));
        }
    }
}
