// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess;

namespace Microsoft.AspNetCore.BenchmarkDotNet.Runner
{
    partial class Program
    {
        private static TextWriter _standardOutput;
        private static StringBuilder _standardOutputText;

        static partial void BeforeMain(string[] args);

        private static int Main(string[] args)
        {
            BeforeMain(args);

            CheckValidate(ref args);
            var summaries = BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly)
                .Run(args, ManualConfig.CreateEmpty());

            foreach (var summary in summaries)
            {
                if (summary.HasCriticalValidationErrors)
                {
                    return Fail(summary, nameof(summary.HasCriticalValidationErrors));
                }

                foreach (var report in summary.Reports)
                {
                    if (!report.BuildResult.IsGenerateSuccess)
                    {
                        return Fail(report, nameof(report.BuildResult.IsGenerateSuccess));
                    }

                    if (!report.BuildResult.IsBuildSuccess)
                    {
                        return Fail(report, nameof(report.BuildResult.IsBuildSuccess));
                    }

                    if (!report.AllMeasurements.Any())
                    {
                        return Fail(report, nameof(report.AllMeasurements));
                    }
                }
            }

            return 0;
        }

        private static int Fail(object o, string message)
        {
            _standardOutput?.WriteLine(_standardOutputText.ToString());

            Console.Error.WriteLine("'{0}' failed, reason: '{1}'", o, message);
            return 1;
        }

        private static void CheckValidate(ref string[] args)
        {
            var argsList = args.ToList();
            if (argsList.Remove("--validate") || argsList.Remove("--validate-fast"))
            {
                SuppressConsole();
                AspNetCoreBenchmarkAttribute.UseValidationConfig = true;
            }

            args = argsList.ToArray();
        }

        private static void SuppressConsole()
        {
            _standardOutput = Console.Out;
            _standardOutputText = new StringBuilder();
            Console.SetOut(new StringWriter(_standardOutputText));
        }
    }
}
