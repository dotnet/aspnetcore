// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Core;
using BenchmarkDotNet.Toolchains.DotNetCli;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MsBuildToolchain : Toolchain
    {
        private const string TargetFrameworkMoniker = "netcoreapp1.1";

        public MsBuildToolchain()
            : base("MsBuildCore",
                   new MsBuildGenerator(),
                   new DotNetCliBuilder(TargetFrameworkMoniker),
                   new MsBuildExecutor())
        {
        }
    }
}