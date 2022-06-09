// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
{
    public class FileLoggerProcessorTests
    {

        private string _messageOne = "Message one";
        private string _messageTwo = "Message two";
        private string _messageThree = "Message three";
        private string _messageFour = "Message four";
        private readonly DateTime _today = new DateTime(2021, 01, 01, 12, 00, 00);

        public FileLoggerProcessorTests()
        {
            TempPath = Path.GetTempFileName() + "_";
        }

        public string TempPath { get; }

        [Fact]
        public async Task WritesToTextFile()
        {
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };
            var path = Path.Combine(TempPath, Path.GetRandomFileName());

            try
            {
                string fileName;
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path
                };
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);
                    fileName = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0000.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(fileName, _messageOne.Length).DefaultTimeout();
                }
                Assert.True(File.Exists(fileName));

                Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(fileName));
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [Fact]
        public async Task RollsTextFilesBasedOnDate()
        {
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };
            var tomorrow = _today.AddDays(1);

            var path = Path.Combine(TempPath, Path.GetRandomFileName());
            var options = new W3CLoggerOptions()
            {
                LogDirectory = path
            };

            try
            {
                string fileNameToday;
                string fileNameTomorrow;

                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);

                    fileNameToday = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0000.txt"));

                    await WaitForFile(fileNameToday, _messageOne.Length).DefaultTimeout();

                    mockSystemDateTime.Now = tomorrow;
                    logger.EnqueueMessage(_messageTwo);

                    fileNameTomorrow = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{tomorrow.Year:0000}{tomorrow.Month:00}{tomorrow.Day:00}.0000.txt"));

                    await WaitForFile(fileNameTomorrow, _messageTwo.Length).DefaultTimeout();
                }

                Assert.True(File.Exists(fileNameToday));
                Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(fileNameToday));
                Assert.True(File.Exists(fileNameTomorrow));
                Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(fileNameTomorrow));
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/34284")]
        [Fact]
        public async Task RollsTextFilesBasedOnSize()
        {
            var path = Path.Combine(TempPath, Path.GetRandomFileName());

            try
            {
                string fileName1;
                string fileName2;
                var mockSystemDateTime = new MockSystemDateTime
                {
                    Now = _today
                };
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    FileSizeLimit = 5
                };
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);
                    logger.EnqueueMessage(_messageTwo);
                    fileName1 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0000.txt"));
                    fileName2 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0001.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(fileName2, _messageTwo.Length).DefaultTimeout();
                }
                Assert.True(File.Exists(fileName1));
                Assert.True(File.Exists(fileName2));

                Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(fileName1));
                Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(fileName2));
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [Fact]
        public async Task RespectsMaxFileCount()
        {
            var path = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "randomFile.txt"), "Text");
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };

            try
            {
                string lastFileName;
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    RetainedFileCountLimit = 3,
                    FileSizeLimit = 5
                };
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    for (int i = 0; i < 10; i++)
                    {
                        logger.EnqueueMessage(_messageOne);
                    }
                    lastFileName = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0009.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(lastFileName, _messageOne.Length).DefaultTimeout();
                    for (int i = 0; i < 6; i++)
                    {
                        await WaitForRoll(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.{i:0000}.txt"))).DefaultTimeout();
                    }
                }

                var actualFiles = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(4, actualFiles.Length);
                Assert.Equal("randomFile.txt", actualFiles[0]);
                for (int i = 1; i < 4; i++)
                {
                    Assert.True((actualFiles[i].StartsWith($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}", StringComparison.InvariantCulture)));
                }
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [Fact]
        public async Task StopsLoggingAfter10000Files()
        {
            var path = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };

            try
            {
                string lastFileName;
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    FileSizeLimit = 5,
                    RetainedFileCountLimit = 10000
                };
                var testSink = new TestSink();
                var testLogger = new TestLoggerFactory(testSink, enabled:true);
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), testLogger))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    for (int i = 0; i < 10000; i++)
                    {
                        logger.EnqueueMessage(_messageOne);
                    }
                    lastFileName = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.9999.txt"));
                    await WaitForFile(lastFileName, _messageOne.Length).DefaultTimeout();

                    // directory is full, no warnings yet
                    Assert.Equal(0, testSink.Writes.Count);

                    logger.EnqueueMessage(_messageOne);
                    await WaitForCondition(() => testSink.Writes.FirstOrDefault()?.EventId.Name == "MaxFilesReached").DefaultTimeout();
                }

                Assert.Equal(10000, new DirectoryInfo(path)
                    .GetFiles()
                    .ToArray().Length);

                // restarting the logger should do nothing since the folder is still full
                var testSink2 = new TestSink();
                var testLogger2 = new TestLoggerFactory(testSink2, enabled:true);
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), testLogger2))
                {
                    Assert.Equal(0, testSink2.Writes.Count);

                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);
                    await WaitForCondition(() => testSink2.Writes.FirstOrDefault()?.EventId.Name == "MaxFilesReached").DefaultTimeout();
                }
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [Fact]
        public async Task InstancesWriteToSameDirectory()
        {
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };

            var path = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);

            try
            {
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    RetainedFileCountLimit = 10,
                    FileSizeLimit = 5
                };
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    for (int i = 0; i < 3; i++)
                    {
                        logger.EnqueueMessage(_messageOne);
                    }
                    var filePath = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0002.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(filePath, _messageOne.Length).DefaultTimeout();
                }

                // Second instance should pick up where first one left off
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    for (int i = 0; i < 3; i++)
                    {
                        logger.EnqueueMessage(_messageOne);
                    }
                    var filePath = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0005.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(filePath, _messageOne.Length).DefaultTimeout();
                }

                var actualFiles1 = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(6, actualFiles1.Length);
                for (int i = 0; i < 6; i++)
                {
                    Assert.Contains($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.{i:0000}.txt", actualFiles1[i]);
                }

                // Third instance should roll to 5 most recent files
                options.RetainedFileCountLimit = 5;
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0006.txt")), _messageOne.Length).DefaultTimeout();
                    await WaitForRoll(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0000.txt"))).DefaultTimeout();
                    await WaitForRoll(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0001.txt"))).DefaultTimeout();
                }

                var actualFiles2 = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(5, actualFiles2.Length);
                for (int i = 0; i < 5; i++)
                {
                    Assert.Equal($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.{i + 2:0000}.txt", actualFiles2[i]);
                }
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/34986")]
        [Fact]
        public async Task WritesToNewFileOnNewInstance()
        {
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };

            var path = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);

            try
            {
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    FileSizeLimit = 5
                };
                var fileName1 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0000.txt"));
                var fileName2 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0001.txt"));
                var fileName3 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0002.txt"));

                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);
                    logger.EnqueueMessage(_messageTwo);
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(fileName2, _messageTwo.Length).DefaultTimeout();
                }

                // Even with a big enough FileSizeLimit, we still won't try to write to files from a previous instance.
                options.FileSizeLimit = 10000;

                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageThree);
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(fileName3, _messageThree.Length).DefaultTimeout();
                }

                var actualFiles = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(3, actualFiles.Length);

                Assert.True(File.Exists(fileName1));
                Assert.True(File.Exists(fileName2));
                Assert.True(File.Exists(fileName3));

                Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(fileName1));
                Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(fileName2));
                Assert.Equal(_messageThree + Environment.NewLine, File.ReadAllText(fileName3));
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [Fact]
        public async Task RollsTextFilesWhenFirstLogOfDayIsMissing()
        {
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };

            var path = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);

            try
            {
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    FileSizeLimit = 5,
                    RetainedFileCountLimit = 2,
                };
                var fileName1 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0000.txt"));
                var fileName2 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0001.txt"));
                var fileName3 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0002.txt"));
                var fileName4 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0003.txt"));

                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);
                    logger.EnqueueMessage(_messageTwo);
                    logger.EnqueueMessage(_messageThree);
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(fileName3, _messageThree.Length).DefaultTimeout();
                }

                // Even with a big enough FileSizeLimit, we still won't try to write to files from a previous instance.
                options.FileSizeLimit = 10000;

                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageFour);
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(fileName4, _messageFour.Length).DefaultTimeout();
                }

                var actualFiles = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(2, actualFiles.Length);

                Assert.False(File.Exists(fileName1));
                Assert.False(File.Exists(fileName2));
                Assert.True(File.Exists(fileName3));
                Assert.True(File.Exists(fileName4));

                Assert.Equal(_messageThree + Environment.NewLine, File.ReadAllText(fileName3));
                Assert.Equal(_messageFour + Environment.NewLine, File.ReadAllText(fileName4));
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/34982")]
        [Fact]
        public async Task WritesToNewFileOnOptionsChange()
        {
            var mockSystemDateTime = new MockSystemDateTime
            {
                Now = _today
            };

            var path = Path.Combine(TempPath, Path.GetRandomFileName());
            Directory.CreateDirectory(path);

            try
            {
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    LoggingFields = W3CLoggingFields.Time,
                    FileSizeLimit = 10000
                };
                var fileName1 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0000.txt"));
                var fileName2 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{_today.Year:0000}{_today.Month:00}{_today.Day:00}.0001.txt"));
                var monitor = new OptionsWrapperMonitor<W3CLoggerOptions>(options);

                await using (var logger = new FileLoggerProcessor(monitor, new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.SystemDateTime = mockSystemDateTime;
                    logger.EnqueueMessage(_messageOne);
                    await WaitForFile(fileName1, _messageOne.Length).DefaultTimeout();
                    options.LoggingFields = W3CLoggingFields.Date;
                    monitor.InvokeChanged();
                    logger.EnqueueMessage(_messageTwo);
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(fileName2, _messageTwo.Length).DefaultTimeout();
                }

                var actualFiles = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(2, actualFiles.Length);

                Assert.True(File.Exists(fileName1));
                Assert.True(File.Exists(fileName2));

                Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(fileName1));
                Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(fileName2));
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        private async Task WaitForFile(string fileName, int length)
        {
            while (!File.Exists(fileName))
            {
                await Task.Delay(100);
            }
            while (true)
            {
                try
                {
                    if (File.ReadAllText(fileName).Length >= length)
                    {
                        break;
                    }
                }
                catch
                {
                    // Continue
                }
                await Task.Delay(10);
            }
        }

        private async Task WaitForCondition(Func<bool> waitForLog)
        {
            while (!waitForLog())
            {
                await Task.Delay(10);
            }
        }

        private async Task WaitForRoll(string fileName)
        {
            while (File.Exists(fileName))
            {
                await Task.Delay(100);
            }
        }
    }
}
