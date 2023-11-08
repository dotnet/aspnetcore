// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing.Tests;
using Xunit;

namespace Microsoft.AspNetCore.InternalTesting;

public class AssemblyTestLogTests : LoggedTest
{
    [Fact]
    public void FunctionalLogs_LogsPreservedFromNonQuarantinedTest()
    {
    }

    [Fact]
    // Keep this test in quarantine, it verifies that quarantined test logs are preserved
    [QuarantinedTest("No issue")]
    public void FunctionalLogs_LogsPreservedFromQuarantinedTest()
    {
    }

    [Fact]
    public void ForAssembly_ReturnsSameInstanceForSameAssembly()
    {
        Assert.Same(
            AssemblyTestLog.ForAssembly(TestableAssembly.ThisAssembly),
            AssemblyTestLog.ForAssembly(TestableAssembly.ThisAssembly));
    }

    [Fact]
    public Task ForAssemblyWritesToAssemblyBaseDirectory() =>
        RunTestLogFunctionalTest((tempDir) =>
        {
            var logger = LoggerFactory.CreateLogger("Test");

            var assembly = TestableAssembly.Create(typeof(AssemblyTestLog), logDirectory: tempDir);
            var assemblyName = assembly.GetName().Name;
            var testName = $"{TestableAssembly.TestClassName}.{TestableAssembly.TestMethodName}";

            var tfmPath = Path.Combine(tempDir, assemblyName, TestableAssembly.TFM);
            var globalLogPath = Path.Combine(tfmPath, "global.log");
            var testLog = Path.Combine(tfmPath, TestableAssembly.TestClassName, $"{testName}.log");

            using var testAssemblyLog = AssemblyTestLog.ForAssembly(assembly);
            testAssemblyLog.OnCI = true;
            logger.LogInformation("Created test log in {baseDirectory}", tempDir);

            using (testAssemblyLog.StartTestLog(
                output: null,
                className: $"{assemblyName}.{TestableAssembly.TestClassName}",
                loggerFactory: out var testLoggerFactory,
                minLogLevel: LogLevel.Trace,
                testName: testName))
            {
                var testLogger = testLoggerFactory.CreateLogger("TestLogger");
                testLogger.LogInformation("Information!");
                testLogger.LogTrace("Trace!");
            }

            Assert.True(File.Exists(globalLogPath), $"Expected global log file {globalLogPath} to exist.");
            Assert.True(File.Exists(testLog), $"Expected test log file {testLog} to exist.");

            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);
        });

    [Fact]
    public void TestLogWritesToITestOutputHelper()
    {
        var output = new TestTestOutputHelper();

        using var assemblyLog = AssemblyTestLog.Create(TestableAssembly.ThisAssembly, baseDirectory: null);
        using (assemblyLog.StartTestLog(output, "NonExistant.Test.Class", out var loggerFactory))
        {
            var logger = loggerFactory.CreateLogger("TestLogger");
            logger.LogInformation("Information!");

            // Trace is disabled by default
            logger.LogTrace("Trace!");
        }

        var testLogContent = MakeConsistent(output.Output);

        Assert.Equal(
@"[OFFSET] TestLifetime Information: Starting test TestLogWritesToITestOutputHelper at TIMESTAMP
[OFFSET] TestLogger Information: Information!
[OFFSET] TestLifetime Information: Finished test TestLogWritesToITestOutputHelper in DURATION
", testLogContent, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public Task TestLogEscapesIllegalFileNames() =>
        RunTestLogFunctionalTest((tempDir) =>
        {
            var illegalTestName = "T:e/s//t";
            var escapedTestName = "T_e_s_t";

            using var testAssemblyLog = AssemblyTestLog.Create(
                TestableAssembly.ThisAssembly,
                baseDirectory: tempDir);
            using var disposable = testAssemblyLog.StartTestLog(
                output: null,
                className: "FakeTestAssembly.FakeTestClass",
                loggerFactory: out var testLoggerFactory,
                minLogLevel: LogLevel.Trace,
                resolvedTestName: out var resolvedTestname,
                out var _,
                testName: illegalTestName);
            Assert.Equal(escapedTestName, resolvedTestname);
        });

    [Fact]
    public Task TestLogWritesToGlobalLogFile() =>
        RunTestLogFunctionalTest((tempDir) =>
        {
            // Because this test writes to a file, it is a functional test and should be logged
            // but it's also testing the test logging facility. So this is pretty meta ;)
            var logger = LoggerFactory.CreateLogger("Test");

            using (var testAssemblyLog = AssemblyTestLog.Create(
                TestableAssembly.ThisAssembly,
                baseDirectory: tempDir))
            {
                testAssemblyLog.OnCI = false;
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                using (testAssemblyLog.StartTestLog(
                    output: null,
                    className: $"{TestableAssembly.ThisAssemblyName}.FakeTestClass",
                    loggerFactory: out var testLoggerFactory,
                    minLogLevel: LogLevel.Trace,
                    testName: "FakeTestName"))
                {
                    var testLogger = testLoggerFactory.CreateLogger("TestLogger");
                    testLogger.LogInformation("Information!");
                    testLogger.LogTrace("Trace!");
                }
            }

            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            var globalLogPath = Path.Combine(
                tempDir,
                TestableAssembly.ThisAssemblyName,
                TestableAssembly.TFM,
                "global.log");
            var testLog = Path.Combine(
                tempDir,
                TestableAssembly.ThisAssemblyName,
                TestableAssembly.TFM,
                "FakeTestClass",
                "FakeTestName.log");

            Assert.True(File.Exists(globalLogPath), $"Expected global log file {globalLogPath} to exist");
            Assert.True(File.Exists(testLog), $"Expected test log file {testLog} to exist");

            var globalLogContent = MakeConsistent(File.ReadAllText(globalLogPath));
            var testLogContent = MakeConsistent(File.ReadAllText(testLog));

            Assert.Equal(
@"[OFFSET] [GlobalTestLog] [Information] Global Test Logging initialized at TIMESTAMP. Configure the output directory via 'LoggingTestingFileLoggingDirectory' MSBuild property or set 'LoggingTestingDisableFileLogging' to 'true' to disable file logging.
[OFFSET] [GlobalTestLog] [Information] Starting test FakeTestName
[OFFSET] [GlobalTestLog] [Information] Finished test FakeTestName in DURATION
", globalLogContent, ignoreLineEndingDifferences: true);
            Assert.Equal(
@"[OFFSET] [TestLifetime] [Information] Starting test FakeTestName at TIMESTAMP
[OFFSET] [TestLogger] [Information] Information!
[OFFSET] [TestLogger] [Verbose] Trace!
[OFFSET] [TestLifetime] [Information] Finished test FakeTestName in DURATION
", testLogContent, ignoreLineEndingDifferences: true);
        });

    [Fact]
    public Task TestLogCleansLogFiles_AfterSuccessfulRun() =>
        RunTestLogFunctionalTest((tempDir) =>
        {
            var logger = LoggerFactory.CreateLogger("Test");
            var globalLogPath = Path.Combine(
                tempDir,
                TestableAssembly.ThisAssemblyName,
                TestableAssembly.TFM,
                "global.log");
            var testLog = Path.Combine(
                tempDir,
                TestableAssembly.ThisAssemblyName,
                TestableAssembly.TFM,
                "FakeTestClass",
                "FakeTestName.log");

            using (var testAssemblyLog = AssemblyTestLog.Create(
                TestableAssembly.ThisAssembly,
                baseDirectory: tempDir))
            {
                testAssemblyLog.OnCI = true;
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                using (testAssemblyLog.StartTestLog(
                    output: null,
                    className: $"{TestableAssembly.ThisAssemblyName}.FakeTestClass",
                    loggerFactory: out var testLoggerFactory,
                    minLogLevel: LogLevel.Trace,
                    testName: "FakeTestName"))
                {
                    var testLogger = testLoggerFactory.CreateLogger("TestLogger");
                    testLogger.LogInformation("Information!");
                    testLogger.LogTrace("Trace!");
                }

                Assert.True(File.Exists(globalLogPath), $"Expected global log file {globalLogPath} to exist.");
                Assert.True(File.Exists(testLog), $"Expected test log file {testLog} to exist.");
            }

            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            Assert.True(!File.Exists(globalLogPath), $"Expected no global log file {globalLogPath} to exist.");
            Assert.True(!File.Exists(testLog), $"Expected no test log file {testLog} to exist.");
        });

    [Fact]
    public Task TestLogDoesNotCleanLogFiles_AfterFailedRun() =>
        RunTestLogFunctionalTest((tempDir) =>
        {
            var logger = LoggerFactory.CreateLogger("Test");
            var globalLogPath = Path.Combine(
                tempDir,
                TestableAssembly.ThisAssemblyName,
                TestableAssembly.TFM,
                "global.log");
            var testLog = Path.Combine(
                tempDir,
                TestableAssembly.ThisAssemblyName,
                TestableAssembly.TFM,
                "FakeTestClass",
                "FakeTestName.log");

            using (var testAssemblyLog = AssemblyTestLog.Create(
                TestableAssembly.ThisAssembly,
                baseDirectory: tempDir))
            {
                testAssemblyLog.OnCI = true;
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                using (testAssemblyLog.StartTestLog(
                    output: null,
                    className: $"{TestableAssembly.ThisAssemblyName}.FakeTestClass",
                    loggerFactory: out var testLoggerFactory,
                    minLogLevel: LogLevel.Trace,
                    testName: "FakeTestName"))
                {
                    var testLogger = testLoggerFactory.CreateLogger("TestLogger");
                    testLogger.LogInformation("Information!");
                    testLogger.LogTrace("Trace!");
                }

                testAssemblyLog.ReportTestFailure();
            }

            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            Assert.True(File.Exists(globalLogPath), $"Expected global log file {globalLogPath} to exist.");
            Assert.True(File.Exists(testLog), $"Expected test log file {testLog} to exist.");
        });

    [Fact]
    public Task TestLogTruncatesTestNameToAvoidLongPaths() =>
        RunTestLogFunctionalTest((tempDir) =>
        {
            var longTestName = new string('0', 50) + new string('1', 50) + new string('2', 50) +
                new string('3', 50) + new string('4', 50);
            var logger = LoggerFactory.CreateLogger("Test");
            using (var testAssemblyLog = AssemblyTestLog.Create(
                TestableAssembly.ThisAssembly,
                baseDirectory: tempDir))
            {
                testAssemblyLog.OnCI = false;
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                using (testAssemblyLog.StartTestLog(
                    output: null,
                    className: $"{TestableAssembly.ThisAssemblyName}.FakeTestClass",
                    loggerFactory: out var testLoggerFactory,
                    minLogLevel: LogLevel.Trace,
                    testName: longTestName))
                {
                    testLoggerFactory.CreateLogger("TestLogger").LogInformation("Information!");
                }
            }

            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            var testLogFiles = new DirectoryInfo(
                Path.Combine(tempDir, TestableAssembly.ThisAssemblyName, TestableAssembly.TFM, "FakeTestClass"))
                .EnumerateFiles();
            var testLog = Assert.Single(testLogFiles);
            var testFileName = Path.GetFileNameWithoutExtension(testLog.Name);

            // The first half of the file comes from the beginning of the test name passed to the logger
            Assert.Equal(
                longTestName.Substring(0, testFileName.Length / 2),
                testFileName.Substring(0, testFileName.Length / 2));

            // The last half of the file comes from the ending of the test name passed to the logger
            Assert.Equal(
                longTestName.Substring(longTestName.Length - testFileName.Length / 2, testFileName.Length / 2),
                testFileName.Substring(testFileName.Length - testFileName.Length / 2, testFileName.Length / 2));
        });

    [Fact]
    public Task TestLogEnumerateFilenamesToAvoidCollisions() =>
        RunTestLogFunctionalTest((tempDir) =>
        {
            var logger = LoggerFactory.CreateLogger("Test");
            using (var testAssemblyLog = AssemblyTestLog.Create(
                TestableAssembly.ThisAssembly,
                baseDirectory: tempDir))
            {
                testAssemblyLog.OnCI = false;
                logger.LogInformation("Created test log in {baseDirectory}", tempDir);

                for (var i = 0; i < 10; i++)
                {
                    using (testAssemblyLog.StartTestLog(
                        output: null,
                        className: $"{TestableAssembly.ThisAssemblyName}.FakeTestClass",
                        loggerFactory: out var testLoggerFactory,
                        minLogLevel: LogLevel.Trace,
                        testName: "FakeTestName"))
                    {
                        testLoggerFactory.CreateLogger("TestLogger").LogInformation("Information!");
                    }
                }
            }

            logger.LogInformation("Finished test log in {baseDirectory}", tempDir);

            // The first log file exists
            Assert.True(File.Exists(Path.Combine(
                tempDir,
                TestableAssembly.ThisAssemblyName,
                TestableAssembly.TFM,
                "FakeTestClass",
                "FakeTestName.log")));

            // Subsequent files exist
            for (var i = 0; i < 9; i++)
            {
                Assert.True(File.Exists(Path.Combine(
                    tempDir,
                    TestableAssembly.ThisAssemblyName,
                    TestableAssembly.TFM,
                    "FakeTestClass",
                    $"FakeTestName.{i}.log")));
            }
        });

    private static readonly Regex TimestampRegex = new(@"\d+-\d+-\d+T\d+:\d+:\d+");
    private static readonly Regex TimestampOffsetRegex = new(@"\d+\.\d+s");
    private static readonly Regex DurationRegex = new(@"[^ ]+s$");

    private static async Task RunTestLogFunctionalTest(Action<string> action)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"TestLogging_{Guid.NewGuid():N}");
        try
        {
            action(tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch
                {
                    await Task.Delay(100);
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }
    }

    private static string MakeConsistent(string input)
    {
        return string.Join(Environment.NewLine, input.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
            .Select(line =>
            {
                var strippedPrefix = line.Contains('[') ? line.Substring(line.IndexOf('[')) : line;

                var strippedDuration = DurationRegex.Replace(strippedPrefix, "DURATION");
                var strippedTimestamp = TimestampRegex.Replace(strippedDuration, "TIMESTAMP");
                var strippedTimestampOffset = TimestampOffsetRegex.Replace(strippedTimestamp, "OFFSET");
                return strippedTimestampOffset;
            }));
    }
}
