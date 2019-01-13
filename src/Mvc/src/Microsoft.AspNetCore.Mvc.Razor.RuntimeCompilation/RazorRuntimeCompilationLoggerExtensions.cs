// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    internal static class MvcRazorLoggerExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private static readonly Action<ILogger, string, Exception> _generatedCodeToAssemblyCompilationStart;
        private static readonly Action<ILogger, string, double, Exception> _generatedCodeToAssemblyCompilationEnd;
        private static readonly Action<ILogger, string, string[], Exception> _malformedPageDirective;
        private static readonly Action<ILogger, string, Exception> _viewCompilerLocatedCompiledView;
        private static readonly Action<ILogger, Exception> _viewCompilerNoCompiledViewsFound;
        private static readonly Action<ILogger, string, Exception> _viewCompilerLocatedCompiledViewForPath;
        private static readonly Action<ILogger, string, Exception> _viewCompilerRecompilingCompiledView;
        private static readonly Action<ILogger, string, Exception> _viewCompilerCouldNotFindFileToCompileForPath;
        private static readonly Action<ILogger, string, Exception> _viewCompilerFoundFileToCompileForPath;
        private static readonly Action<ILogger, string, Exception> _viewCompilerInvalidatingCompiledFile;

        private static readonly Action<ILogger, string, string, Exception> _viewLookupCacheMiss;
        private static readonly Action<ILogger, string, string, Exception> _viewLookupCacheHit;
        private static readonly Action<ILogger, string, Exception> _precompiledViewFound;

        static MvcRazorLoggerExtensions()
        {
            _viewCompilerLocatedCompiledView = LoggerMessage.Define<string>(
                LogLevel.Debug,
                3,
                "Initializing Razor view compiler with compiled view: '{ViewName}'.");

            _viewCompilerNoCompiledViewsFound = LoggerMessage.Define(
                LogLevel.Debug,
                4,
                "Initializing Razor view compiler with no compiled views.");

            _viewCompilerLocatedCompiledViewForPath = LoggerMessage.Define<string>(
                LogLevel.Trace,
                5,
                "Located compiled view for view at path '{Path}'.");

            _viewCompilerLocatedCompiledViewForPath = LoggerMessage.Define<string>(
                LogLevel.Trace,
                5,
                "Located compiled view for view at path '{Path}'.");

            _viewCompilerRecompilingCompiledView = LoggerMessage.Define<string>(
                LogLevel.Trace,
                6,
                "Invalidating compiled view for view at path '{Path}'.");

            _viewCompilerCouldNotFindFileToCompileForPath = LoggerMessage.Define<string>(
                LogLevel.Trace,
                7,
                "Could not find a file for view at path '{Path}'.");

            _viewCompilerFoundFileToCompileForPath = LoggerMessage.Define<string>(
                LogLevel.Trace,
                8,
                "Found file at path '{Path}'.");

            _viewCompilerInvalidatingCompiledFile = LoggerMessage.Define<string>(
                LogLevel.Trace,
                9,
                "Invalidating compiled view at path '{Path}' with a file since the checksum did not match.");

            _viewLookupCacheMiss = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                1,
                "View lookup cache miss for view '{ViewName}' in controller '{ControllerName}'.");

            _viewLookupCacheHit = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                2,
                "View lookup cache hit for view '{ViewName}' in controller '{ControllerName}'.");

            _precompiledViewFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                3,
                "Using precompiled view for '{RelativePath}'.");

            _generatedCodeToAssemblyCompilationStart = LoggerMessage.Define<string>(
                LogLevel.Debug,
                1,
                "Compilation of the generated code for the Razor file at '{FilePath}' started.");

            _generatedCodeToAssemblyCompilationEnd = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                2,
                "Compilation of the generated code for the Razor file at '{FilePath}' completed in {ElapsedMilliseconds}ms.");

            _malformedPageDirective = LoggerMessage.Define<string, string[]>(
                LogLevel.Warning,
                new EventId(104, "MalformedPageDirective"),
                "The page directive at '{FilePath}' is malformed. Please fix the following issues: {Diagnostics}");
        }

        public static void ViewCompilerLocatedCompiledView(this ILogger logger, string view)
        {
            _viewCompilerLocatedCompiledView(logger, view, null);
        }

        public static void ViewCompilerNoCompiledViewsFound(this ILogger logger)
        {
            _viewCompilerNoCompiledViewsFound(logger, null);
        }

        public static void ViewCompilerLocatedCompiledViewForPath(this ILogger logger, string path)
        {
            _viewCompilerLocatedCompiledViewForPath(logger, path, null);
        }

        public static void ViewCompilerCouldNotFindFileAtPath(this ILogger logger, string path)
        {
            _viewCompilerCouldNotFindFileToCompileForPath(logger, path, null);
        }

        public static void ViewCompilerFoundFileToCompile(this ILogger logger, string path)
        {
            _viewCompilerFoundFileToCompileForPath(logger, path, null);
        }

        public static void ViewCompilerInvalidingCompiledFile(this ILogger logger, string path)
        {
            _viewCompilerInvalidatingCompiledFile(logger, path, null);
        }

        public static void ViewLookupCacheMiss(this ILogger logger, string viewName, string controllerName)
        {
            _viewLookupCacheMiss(logger, viewName, controllerName, null);
        }

        public static void ViewLookupCacheHit(this ILogger logger, string viewName, string controllerName)
        {
            _viewLookupCacheHit(logger, viewName, controllerName, null);
        }

        public static void PrecompiledViewFound(this ILogger logger, string relativePath)
        {
            _precompiledViewFound(logger, relativePath, null);
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

        public static void MalformedPageDirective(this ILogger logger, string filePath, IList<RazorDiagnostic> diagnostics)
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                var messages = new string[diagnostics.Count];
                for (var i = 0; i < diagnostics.Count; i++)
                {
                    messages[i] = diagnostics[i].GetMessage();
                }

                _malformedPageDirective(logger, filePath, messages, null);
            }
        }
    }
}