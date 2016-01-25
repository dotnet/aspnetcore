// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.Internal
{
    internal static class MvcRazorLoggerExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private static readonly Action<ILogger, string, Exception> _generatedCodeToAssemblyCompilationStart;
        private static readonly Action<ILogger, string, double, Exception> _generatedCodeToAssemblyCompilationEnd;

        private static readonly Action<ILogger, string, Exception> _razorFileToCodeCompilationStart;
        private static readonly Action<ILogger, string, double, Exception> _razorFileToCodeCompilationEnd;

        private static readonly Action<ILogger, string, string, Exception> _viewLookupCacheMiss;
        private static readonly Action<ILogger, string, string, Exception> _viewLookupCacheHit;

        static MvcRazorLoggerExtensions()
        {
            _razorFileToCodeCompilationStart = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "Code generation for the Razor file at '{FilePath}' started.");

            _razorFileToCodeCompilationEnd = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                2,
                "Code generation for the Razor file at '{FilePath}' completed in {ElapsedMilliseconds}ms.");

            _viewLookupCacheMiss = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                1,
                "View lookup cache miss for view '{ViewName}' in controller '{ControllerName}'.");

            _viewLookupCacheHit = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "View lookup cache hit for view '{ViewName}' in controller '{ControllerName}'.");

            _generatedCodeToAssemblyCompilationStart = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "Compilation of the generated code for the Razor file at '{FilePath}' started.");

            _generatedCodeToAssemblyCompilationEnd = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                2,
                "Compilation of the generated code for the Razor file at '{FilePath}' completed in {ElapsedMilliseconds}ms.");
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

        public static void ViewLookupCacheMiss(this ILogger logger, string viewName, string controllerName)
        {
            _viewLookupCacheMiss(logger, viewName, controllerName, null);
        }

        public static void ViewLookupCacheHit(this ILogger logger, string viewName, string controllerName)
        {
            _viewLookupCacheHit(logger, viewName, controllerName, null);
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
