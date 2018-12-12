// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal static class MvcRazorLoggerExtensions
    {
        private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;

        private static readonly Action<ILogger, string, Exception> _generatedCodeToAssemblyCompilationStart;
        private static readonly Action<ILogger, string, double, Exception> _generatedCodeToAssemblyCompilationEnd;

        private static readonly Action<ILogger, string, Exception> _viewCompilerStartCodeGeneration;
        private static readonly Action<ILogger, string, double, Exception> _viewCompilerEndCodeGeneration;
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

        private static readonly Action<ILogger, string, Exception> _tagHelperComponentInitialized;
        private static readonly Action<ILogger, string, Exception> _tagHelperComponentProcessed;


        static MvcRazorLoggerExtensions()
        {
            _viewCompilerStartCodeGeneration = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, "ViewCompilerStartCodeGeneration"),
                "Code generation for the Razor file at '{FilePath}' started.");

            _viewCompilerEndCodeGeneration = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                new EventId(2, "ViewCompilerEndCodeGeneration"),
                "Code generation for the Razor file at '{FilePath}' completed in {ElapsedMilliseconds}ms.");

            _viewCompilerLocatedCompiledView = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "ViewCompilerLocatedCompiledView"),
                "Initializing Razor view compiler with compiled view: '{ViewName}'.");

            _viewCompilerNoCompiledViewsFound = LoggerMessage.Define(
                LogLevel.Debug,
                new EventId(4, "ViewCompilerNoCompiledViewsFound"),
                "Initializing Razor view compiler with no compiled views.");

            _viewCompilerLocatedCompiledViewForPath = LoggerMessage.Define<string>(
                LogLevel.Trace,
                new EventId(5, "ViewCompilerLocatedCompiledViewForPath"),
                "Located compiled view for view at path '{Path}'.");

            _viewCompilerRecompilingCompiledView = LoggerMessage.Define<string>(
                LogLevel.Trace,
                new EventId(6, "ViewCompilerRecompilingCompiledView"),
                "Invalidating compiled view for view at path '{Path}'.");

            _viewCompilerCouldNotFindFileToCompileForPath = LoggerMessage.Define<string>(
                LogLevel.Trace,
                new EventId(7, "ViewCompilerCouldNotFindFileAtPath"),
                "Could not find a file for view at path '{Path}'.");

            _viewCompilerFoundFileToCompileForPath = LoggerMessage.Define<string>(
                LogLevel.Trace,
                new EventId(8, "ViewCompilerFoundFileToCompile"),
                "Found file at path '{Path}'.");

            _viewCompilerInvalidatingCompiledFile = LoggerMessage.Define<string>(
                LogLevel.Trace,
                new EventId(9, "ViewCompilerInvalidingCompiledFile"),
                "Invalidating compiled view at path '{Path}' with a file since the checksum did not match.");

            _viewLookupCacheMiss = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(1, "ViewLookupCacheMiss"),
                "View lookup cache miss for view '{ViewName}' in controller '{ControllerName}'.");

            _viewLookupCacheHit = LoggerMessage.Define<string, string>(
                LogLevel.Debug,
                new EventId(2, "ViewLookupCacheHit"),
                "View lookup cache hit for view '{ViewName}' in controller '{ControllerName}'.");

            _precompiledViewFound = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "PrecompiledViewFound"),
                "Using precompiled view for '{RelativePath}'.");

            _generatedCodeToAssemblyCompilationStart = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(1, "GeneratedCodeToAssemblyCompilationStart"),
                "Compilation of the generated code for the Razor file at '{FilePath}' started.");

            _generatedCodeToAssemblyCompilationEnd = LoggerMessage.Define<string, double>(
                LogLevel.Debug,
                new EventId(2, "GeneratedCodeToAssemblyCompilationEnd"),
                "Compilation of the generated code for the Razor file at '{FilePath}' completed in {ElapsedMilliseconds}ms.");

            _tagHelperComponentInitialized = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(2, "TagHelperComponentInitialized"),
                "Tag helper component '{ComponentName}' initialized.");

            _tagHelperComponentProcessed = LoggerMessage.Define<string>(
                LogLevel.Debug,
                new EventId(3, "TagHelperComponentProcessed"),
                "Tag helper component '{ComponentName}' processed.");
        }

        public static void ViewCompilerStartCodeGeneration(this ILogger logger, string filePath)
        {
            _viewCompilerStartCodeGeneration(logger, filePath, null);
        }

        public static void ViewCompilerEndCodeGeneration(this ILogger logger, string filePath, long startTimestamp)
        {
            // Don't log if logging wasn't enabled at start of request as time will be wildly wrong.
            if (startTimestamp != 0)
            {
                var currentTimestamp = Stopwatch.GetTimestamp();
                var elapsed = new TimeSpan((long)(TimestampToTicks * (currentTimestamp - startTimestamp)));
                _viewCompilerEndCodeGeneration(logger, filePath, elapsed.TotalMilliseconds, null);
            }
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

        public static void TagHelperComponentInitialized(this ILogger logger, string componentName)
        {
            _tagHelperComponentInitialized(logger, componentName, null);
        }

        public static void TagHelperComponentProcessed(this ILogger logger, string componentName)
        {
            _tagHelperComponentProcessed(logger, componentName, null);
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
