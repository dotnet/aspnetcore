using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Serilog;
using Xunit.Abstractions;

namespace ServerComparison.FunctionalTests
{
    public static class TestLoggingUtilities
    {
        public static readonly string TestOutputRoot = Environment.GetEnvironmentVariable("ASPNETCORE_TEST_LOG_DIR");

        public static ILoggerFactory SetUpLogging<TTestClass>(ITestOutputHelper output, [CallerMemberName] string testName = null)
        {
            var loggerFactory = new LoggerFactory();
            loggerFactory.AddXunit(output, LogLevel.Debug);

            if (!string.IsNullOrEmpty(TestOutputRoot))
            {
                var testClass = typeof(TTestClass).GetTypeInfo();
                var testOutputDir = Path.Combine(TestOutputRoot, testClass.Assembly.GetName().Name, testClass.FullName);
                if (!Directory.Exists(testOutputDir))
                {
                    Directory.CreateDirectory(testOutputDir);
                }

                var testOutputFile = Path.Combine(testOutputDir, $"{testName}.log");

                if (File.Exists(testOutputFile))
                {
                    File.Delete(testOutputFile);
                }

                var serilogger = new LoggerConfiguration()
                    .Enrich.FromLogContext()
                    .MinimumLevel.Verbose()
                    .WriteTo.File(testOutputFile, flushToDiskInterval: TimeSpan.FromSeconds(1))
                    .CreateLogger();
                loggerFactory.AddSerilog(serilogger);
            }

            return loggerFactory;
        }
    }
}
