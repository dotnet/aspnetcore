// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

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

            AssignConfiguration(ref args);
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

        private static void AssignConfiguration(ref string[] args)
        {
            var argsList = args.ToList();
            if (argsList.Remove("--validate") || argsList.Remove("--validate-fast"))
            {
                // Compat: support the old style of passing a config that is used by our build system.
                SuppressConsole();
                AspNetCoreBenchmarkAttribute.ConfigName = AspNetCoreBenchmarkAttribute.NamedConfiguration.Validation;
                args = argsList.ToArray();
                return;
            }
            
            var index = argsList.IndexOf("--config");
            if (index >= 0 && index < argsList.Count -1)
            {
                AspNetCoreBenchmarkAttribute.ConfigName = argsList[index + 1];
                argsList.RemoveAt(index + 1);
                argsList.RemoveAt(index);
                args = argsList.ToArray();
                return;
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Using the debug config since you are debugging. I hope that's OK!");
                Console.WriteLine("Specify a configuration with --config <name> to override");
                AspNetCoreBenchmarkAttribute.ConfigName = AspNetCoreBenchmarkAttribute.NamedConfiguration.Debug;
                return;
            }
        }

        private static void SuppressConsole()
        {
            _standardOutput = Console.Out;
            _standardOutputText = new StringBuilder();
            Console.SetOut(new StringWriter(_standardOutputText));
        }
    }
}
