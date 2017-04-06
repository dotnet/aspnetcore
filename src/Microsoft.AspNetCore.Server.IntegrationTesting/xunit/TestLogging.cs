// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Server.IntegrationTesting.xunit
{
    public static class TestLogging
    {
        // Need to qualify because of Serilog.ILogger :(
        private static readonly object _initLock = new object();
        private volatile static Extensions.Logging.ILogger GlobalLogger = null;

        public static readonly string OutputDirectoryEnvironmentVariableName = "ASPNETCORE_TEST_LOG_DIR";
        public static readonly string TestOutputRoot;

        static TestLogging()
        {
            TestOutputRoot = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariableName);
        }

        public static IDisposable Start<TTestClass>(ITestOutputHelper output, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) =>
            Start(output, out loggerFactory, typeof(TTestClass).GetTypeInfo().Assembly.GetName().Name, typeof(TTestClass).FullName, testName);

        public static IDisposable Start(ITestOutputHelper output, out ILoggerFactory loggerFactory, string appName, string className, [CallerMemberName] string testName = null)
        {
            EnsureGlobalLoggingInitialized(appName);

            var factory = CreateLoggerFactory(output, appName, className, testName);
            loggerFactory = factory;
            var logger = factory.CreateLogger("TestLifetime");

            var stopwatch = Stopwatch.StartNew();
            GlobalLogger.LogInformation("Starting test {testName}", testName);
            logger.LogInformation("Starting test {testName}", testName);

            return new Disposable(() =>
            {
                stopwatch.Stop();
                GlobalLogger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                logger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                factory.Dispose();
            });
        }

        public static ILoggerFactory CreateLoggerFactory<TTestClass>(ITestOutputHelper output, [CallerMemberName] string testName = null) =>
            CreateLoggerFactory(output, typeof(TTestClass).GetTypeInfo().Assembly.GetName().Name, typeof(TTestClass).FullName, testName);

        public static ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string appName, string className, [CallerMemberName] string testName = null)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddXunit(output, LogLevel.Debug);

            // Try to shorten the class name using the assembly name
            if (className.StartsWith(appName + "."))
            {
                className = className.Substring(appName.Length + 1);
            }

            var testOutputFile = Path.Combine(appName, className, $"{testName}.log");
            AddFileLogging(loggerFactory, testOutputFile);

            return loggerFactory;
        }

        // Need to qualify because of Serilog.ILogger :(
        private static Extensions.Logging.ILogger CreateGlobalLogger(string assemblyName)
        {
            var loggerFactory = new LoggerFactory();

            // Let the global logger log to the console, it's just "Starting X..." "Finished X..."
            loggerFactory.AddConsole();

            var globalLogFileName = Path.Combine(assemblyName, "global.log");
            AddFileLogging(loggerFactory, globalLogFileName);

            var logger = loggerFactory.CreateLogger("GlobalTestLog");
            logger.LogInformation($"Global Test Logging initialized. Set the '{OutputDirectoryEnvironmentVariableName}' Environment Variable in order to create log files on disk.");
            return logger;
        }

        private static void AddFileLogging(ILoggerFactory loggerFactory, string fileName)
        {
            if (!string.IsNullOrEmpty(TestOutputRoot))
            {
                fileName = Path.Combine(TestOutputRoot, fileName);

                var dir = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }

                var serilogger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.Verbose()
                    .WriteTo.File(fileName, flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
                loggerFactory.AddSerilog(serilogger);
            }
        }

        private static void EnsureGlobalLoggingInitialized(string assemblyName)
        {
            // Ye olde double-check lock because we need to pass the assembly name in if we are initializing
            // so we can't use Lazy<T>
            if(GlobalLogger == null)
            {
                lock(_initLock)
                {
                    if(GlobalLogger == null)
                    {
                        GlobalLogger = CreateGlobalLogger(assemblyName);
                    }
                }
            }
        }

        private class Disposable : IDisposable
        {
            private Action _action;

            public Disposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }

    public abstract class LoggedTest
    {
        private readonly ITestOutputHelper _output;

        protected LoggedTest(ITestOutputHelper output)
        {
            _output = output;
        }

        public IDisposable StartLog(out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null)
        {
            return TestLogging.Start(
                _output,
                out loggerFactory,
                GetType().GetTypeInfo().Assembly.GetName().Name,
                GetType().FullName,
                testName);
        }
    }
}
