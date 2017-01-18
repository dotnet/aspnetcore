// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.DotNetCli;
using BenchmarkDotNet.Toolchains.Results;

namespace Microsoft.AspNetCore.Server.Kestrel.Performance
{
    public class MsBuildExecutor : IExecutor
    {
        public ExecuteResult Execute(BuildResult buildResult, Benchmark benchmark, ILogger logger, IResolver resolver, IDiagnoser diagnoser = null)
        {
            var workingDirectory = buildResult.ArtifactsPaths.BuildArtifactsDirectoryPath;

            using (var process = new Process { StartInfo = BuildDotNetProcessStartInfo(workingDirectory) })
            {
                var loggerDiagnoserType = typeof(Toolchain).GetTypeInfo().Assembly.GetType("BenchmarkDotNet.Loggers.SynchronousProcessOutputLoggerWithDiagnoser");
                var loggerDiagnoser = Activator.CreateInstance(
                    loggerDiagnoserType,
                    new object[] { logger, process, diagnoser, benchmark });

                process.Start();

                var processInputMethodInfo = loggerDiagnoser.GetType().GetMethod("ProcessInput", BindingFlags.Instance | BindingFlags.NonPublic);
                processInputMethodInfo.Invoke(loggerDiagnoser, null);

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return new ExecuteResult(true, process.ExitCode, new string[0], new string[0]);
                }

                var linesWithResults = (IReadOnlyList<string>)loggerDiagnoserType
                    .GetProperty("LinesWithResults", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(loggerDiagnoser, null);
                var linesWithExtraOutput = (IReadOnlyList<string>)loggerDiagnoserType
                    .GetProperty("LinesWithExtraOutput", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(loggerDiagnoser, null);

                return new ExecuteResult(true, process.ExitCode, linesWithResults, linesWithExtraOutput);
            }
        }

        private ProcessStartInfo BuildDotNetProcessStartInfo(string workingDirectory)
            => new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = workingDirectory,
                Arguments = "binaries/Benchmark.Generated.dll",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
    }
}