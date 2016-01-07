// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class RoslynCompilationServiceLoggerExtensions
    {
        private static readonly double TimestampToTicks = Stopwatch.Frequency / 10000000.0;
        private static readonly Action<ILogger, string, Exception> _generatedCodeToAssemblyCompilationStart;
        private static readonly Action<ILogger, string, double, Exception> _generatedCodeToAssemblyCompilationEnd;

        static RoslynCompilationServiceLoggerExtensions()
        {
            _generatedCodeToAssemblyCompilationStart = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "Compilation of the generated code for the Razor file at '{FilePath}' started.");

            _generatedCodeToAssemblyCompilationEnd = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                2,
                "Compilation of the generated code for the Razor file at '{FilePath}' completed in {ElapsedMilliseconds}ms.");
        }

        public static void GeneratedCodeToAssemblyCompilationStart(this ILogger logger, string filePath)
        {
            _generatedCodeToAssemblyCompilationStart(logger, filePath, null);
        }

        public static void GeneratedCodeToAssemblyCompilationEnd(this ILogger logger, string filePath, long startTimestamp)
        {
            // Don't log if logging wasn't enabled at start of request as time will be wildly wrong.
            if (startTimestamp != 0)
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));
                _generatedCodeToAssemblyCompilationEnd(logger, filePath, elapsed.TotalMilliseconds, null);
            }
        }
    }
}
