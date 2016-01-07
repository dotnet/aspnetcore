// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNet.Mvc.Logging
{
    public static class RazorCompilationServiceLoggerExtensions
    {
        private static readonly double TimestampToTicks = Stopwatch.Frequency / 10000000.0;
        private static readonly Action<ILogger, string, Exception> _razorFileToCodeCompilationStart;
        private static readonly Action<ILogger, string, double, Exception> _razorFileToCodeCompilationEnd;

        static RazorCompilationServiceLoggerExtensions()
        {
            _razorFileToCodeCompilationStart = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "Code generation for the Razor file at '{FilePath}' started.");

            _razorFileToCodeCompilationEnd = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                2,
                "Code generation for the Razor file at '{FilePath}' completed in {ElapsedMilliseconds}ms.");
        }

        public static void RazorFileToCodeCompilationStart(this ILogger logger, string filePath)
        {
            _razorFileToCodeCompilationStart(logger, filePath, null);
        }

        public static void RazorFileToCodeCompilationEnd(this ILogger logger, string filePath, long startTimestamp)
        {
            // Don't log if logging wasn't enabled at start of request as time will be wildly wrong.
            if (startTimestamp != 0)
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));
                _razorFileToCodeCompilationEnd(logger, filePath, elapsed.TotalMilliseconds, null);
            }
        }
    }
}
