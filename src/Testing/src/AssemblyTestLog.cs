// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Extensions.Logging;
using Xunit.Abstractions;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Microsoft.AspNetCore.InternalTesting;

public class AssemblyTestLog : IAcceptFailureReports, IDisposable
{
    private const string MaxPathLengthEnvironmentVariableName = "ASPNETCORE_TEST_LOG_MAXPATH";
    private const string LogFileExtension = ".log";
    private static readonly int MaxPathLength = GetMaxPathLength();

    private static readonly object _lock = new();
    private static readonly Dictionary<Assembly, AssemblyTestLog> _logs = new();

    private readonly ILoggerFactory _globalLoggerFactory;
    private readonly ILogger _globalLogger;
    private readonly string _baseDirectory;
    private readonly Assembly _assembly;
    private readonly IServiceProvider _serviceProvider;
    private bool _testFailureReported;

    private static int GetMaxPathLength()
    {
        var maxPathString = Environment.GetEnvironmentVariable(MaxPathLengthEnvironmentVariableName);
        var defaultMaxPath = 245;
        return string.IsNullOrEmpty(maxPathString) ? defaultMaxPath : int.Parse(maxPathString, CultureInfo.InvariantCulture);
    }

    private AssemblyTestLog(ILoggerFactory globalLoggerFactory, ILogger globalLogger, string baseDirectory, Assembly assembly, IServiceProvider serviceProvider)
    {
        _globalLoggerFactory = globalLoggerFactory;
        _globalLogger = globalLogger;
        _baseDirectory = baseDirectory;
        _assembly = assembly;
        _serviceProvider = serviceProvider;
    }

    // internal for testing
    internal bool OnCI { get; set; } = SkipOnCIAttribute.OnCI();

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, [CallerMemberName] string testName = null) =>
        StartTestLog(output, className, out loggerFactory, LogLevel.Debug, testName);

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, LogLevel minLogLevel, [CallerMemberName] string testName = null) =>
        StartTestLog(output, className, out loggerFactory, minLogLevel, out var _, out var _, testName);

    internal IDisposable StartTestLog(ITestOutputHelper output, string className, out ILoggerFactory loggerFactory, LogLevel minLogLevel, out string resolvedTestName, out string logOutputDirectory, [CallerMemberName] string testName = null)
    {
        var logStart = DateTimeOffset.UtcNow;
        var serviceProvider = CreateLoggerServices(output, className, minLogLevel, out resolvedTestName, out logOutputDirectory, testName, logStart);
        var factory = serviceProvider.GetRequiredService<ILoggerFactory>();
        loggerFactory = factory;
        var logger = loggerFactory.CreateLogger("TestLifetime");

        var stopwatch = Stopwatch.StartNew();

        var scope = logger.BeginScope("Test: {testName}", testName);

        _globalLogger.LogInformation("Starting test {testName}", testName);
        logger.LogInformation("Starting test {testName} at {logStart}", testName, logStart.ToString("s", CultureInfo.InvariantCulture));

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

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string className, [CallerMemberName] string testName = null, DateTimeOffset? logStart = null)
        => CreateLoggerFactory(output, className, LogLevel.Trace, testName, logStart);

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public ILoggerFactory CreateLoggerFactory(ITestOutputHelper output, string className, LogLevel minLogLevel, [CallerMemberName] string testName = null, DateTimeOffset? logStart = null)
        => CreateLoggerServices(output, className, minLogLevel, out var _, out var _, testName, logStart).GetRequiredService<ILoggerFactory>();

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public IServiceProvider CreateLoggerServices(ITestOutputHelper output, string className, LogLevel minLogLevel, out string normalizedTestName, [CallerMemberName] string testName = null, DateTimeOffset? logStart = null)
        => CreateLoggerServices(output, className, minLogLevel, out normalizedTestName, out var _, testName, logStart);

    [SuppressMessage("ApiDesign", "RS0026:Do not add multiple public overloads with optional parameters", Justification = "Required to maintain compatibility")]
    public IServiceProvider CreateLoggerServices(ITestOutputHelper output, string className, LogLevel minLogLevel, out string normalizedTestName, out string logOutputDirectory, [CallerMemberName] string testName = null, DateTimeOffset? logStart = null)
    {
        normalizedTestName = string.Empty;
        logOutputDirectory = string.Empty;
        var assemblyName = _assembly.GetName().Name;

        // Try to shorten the class name using the assembly name
        if (className.StartsWith(assemblyName + ".", StringComparison.Ordinal))
        {
            className = className.Substring(assemblyName.Length + 1);
        }

        SerilogLoggerProvider serilogLoggerProvider = null;
        if (!string.IsNullOrEmpty(_baseDirectory))
        {
            logOutputDirectory = Path.Combine(_baseDirectory, className);
            testName = TestFileOutputContext.RemoveIllegalFileChars(testName);

            if (logOutputDirectory.Length + testName.Length + LogFileExtension.Length >= MaxPathLength)
            {
                _globalLogger.LogWarning($"Test name {testName} is too long. Please shorten test name.");

                // Shorten the test name by removing the middle portion of the testname
                var testNameLength = MaxPathLength - logOutputDirectory.Length - LogFileExtension.Length;

                if (testNameLength <= 0)
                {
                    throw new InvalidOperationException("Output file path could not be constructed due to max path length restrictions. Please shorten test assembly, class or method names.");
                }

                testName = string.Concat(testName.AsSpan(0, testNameLength / 2).ToString(), testName.AsSpan(testName.Length - testNameLength / 2, testNameLength / 2).ToString());

                _globalLogger.LogWarning($"To prevent long paths test name was shortened to {testName}.");
            }

            var testOutputFile = Path.Combine(logOutputDirectory, $"{testName}{LogFileExtension}");

            if (File.Exists(testOutputFile))
            {
                _globalLogger.LogWarning($"Output log file {testOutputFile} already exists. Please try to keep log file names unique.");

                for (var i = 0; i < 1000; i++)
                {
                    testOutputFile = Path.Combine(logOutputDirectory, $"{testName}.{i}{LogFileExtension}");

                    if (!File.Exists(testOutputFile))
                    {
                        _globalLogger.LogWarning($"To resolve log file collision, the enumerated file {testOutputFile} will be used.");
                        testName = $"{testName}.{i}";
                        break;
                    }
                }
            }

            normalizedTestName = testName;
            serilogLoggerProvider = ConfigureFileLogging(testOutputFile, logStart);
        }

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddLogging(builder =>
        {
            builder.SetMinimumLevel(minLogLevel);

            if (output != null)
            {
                builder.AddXunit(output, minLogLevel, logStart);
            }

            if (serilogLoggerProvider != null)
            {
                // Use a factory so that the container will dispose it
                builder.Services.AddSingleton<ILoggerProvider>(_ => serilogLoggerProvider);
            }
        });

        return serviceCollection.BuildServiceProvider();
    }

    // internal for testing. Expectation is AspNetTestAssembly runner calls ForAssembly() first for every Assembly.
    internal static AssemblyTestLog Create(Assembly assembly, string baseDirectory)
    {
        var logStart = DateTimeOffset.UtcNow;
        SerilogLoggerProvider serilogLoggerProvider = null;
        if (!string.IsNullOrEmpty(baseDirectory))
        {
            baseDirectory = TestFileOutputContext.GetAssemblyBaseDirectory(assembly, baseDirectory);
            var globalLogFileName = Path.Combine(baseDirectory, "global.log");
            serilogLoggerProvider = ConfigureFileLogging(globalLogFileName, logStart);
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
        logger.LogInformation("Global Test Logging initialized at {logStart}. "
            + "Configure the output directory via 'LoggingTestingFileLoggingDirectory' MSBuild property "
            + "or set 'LoggingTestingDisableFileLogging' to 'true' to disable file logging.",
            logStart.ToString("s", CultureInfo.InvariantCulture));
        return new AssemblyTestLog(loggerFactory, logger, baseDirectory, assembly, serviceProvider);
    }

    public static AssemblyTestLog ForAssembly(Assembly assembly)
    {
        lock (_lock)
        {
            if (!_logs.TryGetValue(assembly, out var log))
            {
                var stackTrace = Environment.StackTrace;
                if (!stackTrace.Contains(
                    "Microsoft.AspNetCore.InternalTesting"
#if NETCOREAPP
                    , StringComparison.Ordinal
#endif
                    ))
                {
                    throw new InvalidOperationException($"Unexpected initial {nameof(ForAssembly)} caller.");
                }

                var baseDirectory = TestFileOutputContext.GetOutputDirectory(assembly);

                // Try to clear previous logs, continue if it fails. Do this before creating new global logger.
                var assemblyBaseDirectory = TestFileOutputContext.GetAssemblyBaseDirectory(assembly);
                if (!string.IsNullOrEmpty(assemblyBaseDirectory) &&
                    !TestFileOutputContext.GetPreserveExistingLogsInOutput(assembly))
                {
                    try
                    {
                        Directory.Delete(assemblyBaseDirectory, recursive: true);
                    }
                    catch
                    {
                    }
                }

                log = Create(assembly, baseDirectory);
                _logs[assembly] = log;
            }

            return log;
        }
    }

    public void ReportTestFailure()
    {
        _testFailureReported = true;
    }

    private static TestFrameworkFileLoggerAttribute GetFileLoggerAttribute(Assembly assembly)
        => assembly.GetCustomAttribute<TestFrameworkFileLoggerAttribute>()
            ?? throw new InvalidOperationException($"No {nameof(TestFrameworkFileLoggerAttribute)} found on the assembly {assembly.GetName().Name}. "
                + "The attribute is added via msbuild properties of the Microsoft.Extensions.Logging.Testing. "
                + "Please ensure the msbuild property is imported or a direct reference to Microsoft.Extensions.Logging.Testing is added.");

    private static SerilogLoggerProvider ConfigureFileLogging(string fileName, DateTimeOffset? logStart)
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
            .Enrich.With(new AssemblyLogTimestampOffsetEnricher(logStart))
            .MinimumLevel.Verbose()
            .WriteTo.File(fileName,
                outputTemplate: "[{TimestampOffset}] [{SourceContext}] [{Level}] {Message:l}{NewLine}{Exception}",
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                shared: true,
                formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        return new SerilogLoggerProvider(serilogger, dispose: true);
    }

    void IDisposable.Dispose()
    {
        (_serviceProvider as IDisposable)?.Dispose();
        _globalLoggerFactory.Dispose();

        // Clean up if no tests failed and we're not running local tests. (Ignoring tests of this class, OnCI is
        // true on both build and Helix agents.) In particular, remove the directory containing the global.log
        // file. All test class log files for this assembly are in subdirectories of this location.
        if (!_testFailureReported &&
            OnCI &&
            _baseDirectory is not null &&
            Directory.Exists(_baseDirectory))
        {
            try
            {
                Directory.Delete(_baseDirectory, recursive: true);
            }
            catch
            {
                // Best effort. Ignore problems deleting locked logged files.
            }
        }
    }

    private sealed class AssemblyLogTimestampOffsetEnricher : ILogEventEnricher
    {
        private readonly DateTimeOffset? _logStart;

        public AssemblyLogTimestampOffsetEnricher(DateTimeOffset? logStart)
        {
            _logStart = logStart;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
            => logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(
                    "TimestampOffset",
                    _logStart.HasValue
                        ? $"{(DateTimeOffset.UtcNow - _logStart.Value).TotalSeconds.ToString("N3", CultureInfo.InvariantCulture)}s"
                        : DateTimeOffset.UtcNow.ToString("s", CultureInfo.InvariantCulture)));
    }

    private sealed class Disposable : IDisposable
    {
        private readonly Action _action;

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
