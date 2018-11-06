// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Logging.Testing
{
    public class AssemblyTestLog : IDisposable
    {
        public static readonly string OutputDirectoryEnvironmentVariableName = "ASPNETCORE_TEST_LOG_DIR";
        private static readonly string MaxPathLengthEnvironmentVariableName = "ASPNETCORE_TEST_LOG_MAXPATH";
        private static readonly string LogFileExtension = ".log";
        private static readonly int MaxPathLength = GetMaxPathLength();
        private static char[] InvalidFileChars = new char[]
        {
            '\"', '<', '>', '|', '\0',
            (char)1, (char)2, (char)3, (char)4, (char)5, (char)6, (char)7, (char)8, (char)9, (char)10,
            (char)11, (char)12, (char)13, (char)14, (char)15, (char)16, (char)17, (char)18, (char)19, (char)20,
            (char)21, (char)22, (char)23, (char)24, (char)25, (char)26, (char)27, (char)28, (char)29, (char)30,
            (char)31, ':', '*', '?', '\\', '/', ' ', (char)127
        };

        private static readonly object _lock = new object();
        private static readonly Dictionary<Assembly, AssemblyTestLog> _logs = new Dictionary<Assembly, AssemblyTestLog>();

        private readonly ILoggerFactory _globalLoggerFactory;
        private readonly ILogger _globalLogger;
        private readonly string _baseDirectory;
        private readonly string _assemblyName;
        private readonly IServiceProvider _serviceProvider;

        private static int GetMaxPathLength()
        {
            var maxPathString = Environment.GetEnvironmentVariable(MaxPathLengthEnvironmentVariableName);
            var defaultMaxPath = 245;
            return string.IsNullOrEmpty(maxPathString) ? defaultMaxPath : int.Parse(maxPathString);
        }

        private AssemblyTestLog(ILoggerFactory globalLoggerFactory, ILogger globalLogger, string baseDirectory, string assemblyName, IServiceProvider serviceProvider)
        {
            _globalLoggerFactory = globalLoggerFactory;
            _globalLogger = globalLogger;
            _baseDirectory = baseDirectory;
            _assemblyName = assemblyName;
            _serviceProvider = serviceProvider;
        }

        public IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) =>
            StartTestLog(output, className, out loggerFactory, LogLevel.Debug, testName);

        public IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null) =>
            StartTestLog(output, className, out loggerFactory, minLogLevel, out var _, testName);

        internal IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, LogLevel minLogLevel, out string resolvedTestName, [CallerMemberName] string testName = null)
        {
            var serviceProvider = CreateLoggerServices(output, className, minLogLevel, out resolvedTestName, testName);
            var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
            loggerFactory = factory;
            var logger = loggerFactory.CreateLogger("TestLifetime");

            var stopwatch = Stopwatch.StartNew();

            var scope = logger.BeginScope("Test: {testName}", testName);

            _globalLogger.LogInformation("Starting test {testName}", testName);
            logger.LogInformation("Starting test {testName}", testName);

            return new Disposable(() =>
            {
                stopwatch.Stop();
                _globalLogger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                logger.LogInformation("Finished test {testName} in {duration}s", testName, stopwatch.Elapsed.TotalSeconds);
                scope.Dispose();
                factory.Dispose();
                (serviceProvider as IDisposable)?.Dispose();
            });
        }

        public ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string className, [CallerMemberName] string testName = null) =>
            CreateLoggerFactory(output, className, LogLevel.Trace, testName);

        public ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string className, LogLevel minLogLevel, [CallerMemberName] string testName = null)
        {
            return CreateLoggerServices(output, className, minLogLevel, out var _, testName).GetRequiredService<ILoggerFactory>();
        }

        public IServiceProvider CreateLoggerServices(ITestOutputHelper output, string className, LogLevel minLogLevel, out string normalizedTestName, [CallerMemberName] string testName = null)
        {
            normalizedTestName = string.Empty;

            // Try to shorten the class name using the assembly name
            if (className.StartsWith(_assemblyName + "."))
            {
                className = className.Substring(_assemblyName.Length + 1);
            }

            SerilogLoggerProvider serilogLoggerProvider = null;
            if (!string.IsNullOrEmpty(_baseDirectory))
            {
                var testOutputDirectory = Path.Combine(GetAssemblyBaseDirectory(_assemblyName, _baseDirectory), className);
                testName = RemoveIllegalFileChars(testName);

                if (testOutputDirectory.Length + testName.Length + LogFileExtension.Length >= MaxPathLength)
                {
                    _globalLogger.LogWarning($"Test name {testName} is too long. Please shorten test name.");

                    // Shorten the test name by removing the middle portion of the testname
                    var testNameLength = MaxPathLength - testOutputDirectory.Length - LogFileExtension.Length;

                    if (testNameLength <= 0)
                    {
                        throw new InvalidOperationException("Output file path could not be constructed due to max path length restrictions. Please shorten test assembly, class or method names.");
                    }

                    testName = testName.Substring(0, testNameLength / 2) + testName.Substring(testName.Length - testNameLength / 2, testNameLength / 2);

                    _globalLogger.LogWarning($"To prevent long paths test name was shortened to {testName}.");
                }

                var testOutputFile = Path.Combine(testOutputDirectory, $"{testName}{LogFileExtension}");

                if (File.Exists(testOutputFile))
                {
                    _globalLogger.LogWarning($"Output log file {testOutputFile} already exists. Please try to keep log file names unique.");

                    for (var i = 0; i < 1000; i++)
                    {
                        testOutputFile = Path.Combine(testOutputDirectory, $"{testName}.{i}{LogFileExtension}");

                        if (!File.Exists(testOutputFile))
                        {
                            _globalLogger.LogWarning($"To resolve log file collision, the enumerated file {testOutputFile} will be used.");
                            testName = $"{testName}.{i}";
                            break;
                        }
                    }
                }

                normalizedTestName = testName;
                serilogLoggerProvider = ConfigureFileLogging(testOutputFile);
            }

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddLogging(builder =>
            {
                builder.SetMinimumLevel(minLogLevel);

                if (output != null)
                {
                    builder.AddXunit(output, minLogLevel);
                }

                if (serilogLoggerProvider != null)
                {
                    // Use a factory so that the container will dispose it
                    builder.Services.AddSingleton<ILoggerProvider>(_ => serilogLoggerProvider);
                }
            });

            return serviceCollection.BuildServiceProvider();
        }

        public static AssemblyTestLog Create(string assemblyName, string baseDirectory)
        {
            SerilogLoggerProvider serilogLoggerProvider = null;
            var globalLogDirectory = GetAssemblyBaseDirectory(assemblyName, baseDirectory);
            if (!string.IsNullOrEmpty(globalLogDirectory))
            {
                var globalLogFileName = Path.Combine(globalLogDirectory, "global.log");
                serilogLoggerProvider = ConfigureFileLogging(globalLogFileName);
            }

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddLogging(builder =>
            {
                // Global logging, when it's written, is expected to be outputted. So set the log level to minimum.
                builder.SetMinimumLevel(LogLevel.Trace);

                if (serilogLoggerProvider != null)
                {
                    // Use a factory so that the container will dispose it
                    builder.Services.AddSingleton<ILoggerProvider>(_ => serilogLoggerProvider);
                }
            });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var logger = loggerFactory.CreateLogger("GlobalTestLog");
            logger.LogInformation($"Global Test Logging initialized. Set the '{OutputDirectoryEnvironmentVariableName}' Environment Variable in order to create log files on disk.");
            return new AssemblyTestLog(loggerFactory, logger, baseDirectory, assemblyName, serviceProvider);
        }

        public static AssemblyTestLog ForAssembly(Assembly assembly)
        {
            lock (_lock)
            {
                if (!_logs.TryGetValue(assembly, out var log))
                {
                    var assemblyName = assembly.GetName().Name;
                    var baseDirectory = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariableName);
                    log = Create(assemblyName, baseDirectory);
                    _logs[assembly] = log;

                    // Try to clear previous logs
                    var assemblyBaseDirectory = GetAssemblyBaseDirectory(assemblyName, baseDirectory);
                    if (Directory.Exists(assemblyBaseDirectory))
                    {
                        try
                        {
                            Directory.Delete(assemblyBaseDirectory, recursive: true);
                        }
                        catch {}
                    }
                }
                return log;
            }
        }

        private static string GetAssemblyBaseDirectory(string assemblyName, string baseDirectory)
        {
            if (!string.IsNullOrEmpty(baseDirectory))
            {
                return Path.Combine(baseDirectory, assemblyName, RuntimeInformation.FrameworkDescription.TrimStart('.'));
            }
            return string.Empty;
        }

        private static SerilogLoggerProvider ConfigureFileLogging(string fileName)
        {
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
                .WriteTo.File(fileName, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{SourceContext}] [{Level}] {Message}{NewLine}{Exception}", flushToDiskInterval: TimeSpan.FromSeconds(1), shared: true)
                .CreateLogger();
            return new SerilogLoggerProvider(serilogger, dispose: true);
        }

        private static string RemoveIllegalFileChars(string s)
        {
            var sb = new StringBuilder();

            foreach (var c in s)
            {
                if (InvalidFileChars.Contains(c))
                {
                    if (sb.Length > 0 && sb[sb.Length - 1] != '_')
                    {
                        sb.Append('_');
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public void Dispose()
        {
            (_serviceProvider as IDisposable)?.Dispose();
            _globalLoggerFactory.Dispose();
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
}
