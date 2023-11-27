// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;

namespace Microsoft.AspNetCore.HttpLogging;

public class FileLoggerProcessorTests
{
    private string _messageOne = "Message one";
    private string _messageTwo = "Message two";
    private string _messageThree = "Message three";
    private string _messageFour = "Message four";

    private DateTime _today = new DateTime(2021, 01, 01, 12, 00, 00);

    public FileLoggerProcessorTests()
    {
        TempPath = Path.Combine(Environment.CurrentDirectory, "_");
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
            string filePath;
            var options = new W3CLoggerOptions()
            {
                LogDirectory = path
            };
            await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageOne);
                filePath = GetLogFilePath(path, options.FileName, _today, 0);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(filePath, _messageOne.Length).DefaultTimeout();
            }
            Assert.True(File.Exists(filePath));

            Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(filePath));
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
            string filePathToday;
            string filePathTomorrow;

            await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageOne);

                filePathToday = GetLogFilePath(path, options.FileName, _today, 0);

                await WaitForFile(filePathToday, _messageOne.Length).DefaultTimeout();

                mockSystemDateTime.Now = tomorrow;
                logger.EnqueueMessage(_messageTwo);

                filePathTomorrow = GetLogFilePath(path, options.FileName, tomorrow, 0);

                await WaitForFile(filePathTomorrow, _messageTwo.Length).DefaultTimeout();
            }

            Assert.True(File.Exists(filePathToday));
            Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(filePathToday));
            Assert.True(File.Exists(filePathTomorrow));
            Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(filePathTomorrow));
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    [Fact]
    public async Task RollsTextFilesBasedOnSize()
    {
        var path = Path.Combine(TempPath, Path.GetRandomFileName());

        try
        {
            string filePath1;
            string filePath2;
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
                filePath1 = GetLogFilePath(path, options.FileName, _today, 0);
                filePath2 = GetLogFilePath(path, options.FileName, _today, 1);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(filePath2, _messageTwo.Length).DefaultTimeout();
            }
            Assert.True(File.Exists(filePath1));
            Assert.True(File.Exists(filePath2));

            Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(filePath1));
            Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(filePath2));
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
            string lastFilePath;
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
                lastFilePath = GetLogFilePath(path, options.FileName, _today, 9);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(lastFilePath, _messageOne.Length).DefaultTimeout();
                for (int i = 0; i < 6; i++)
                {
                    await WaitForRoll(GetLogFilePath(path, options.FileName, _today, i)).DefaultTimeout();
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
                Assert.StartsWith(GetLogFileBaseName(options.FileName, _today), actualFiles[i], StringComparison.InvariantCulture);
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
            string lastFilePath;
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
                lastFilePath = GetLogFilePath(path, options.FileName, _today, 9999);
                await WaitForFile(lastFilePath, _messageOne.Length).DefaultTimeout();

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
                var filePath = GetLogFilePath(path, options.FileName, _today, 2);
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
                var filePath = GetLogFilePath(path, options.FileName, _today, 5);
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
                Assert.Contains(GetLogFileName(options.FileName, _today, i), actualFiles1[i]);
            }

            // Third instance should roll to 5 most recent files
            options.RetainedFileCountLimit = 5;
            await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageOne);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(GetLogFilePath(path, options.FileName, _today, 6), _messageOne.Length).DefaultTimeout();
                await WaitForRoll(GetLogFilePath(path, options.FileName, _today, 0)).DefaultTimeout();
                await WaitForRoll(GetLogFilePath(path, options.FileName, _today, 1)).DefaultTimeout();
            }

            var actualFiles2 = new DirectoryInfo(path)
                .GetFiles()
                .Select(f => f.Name)
                .OrderBy(f => f)
                .ToArray();

            Assert.Equal(5, actualFiles2.Length);
            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(GetLogFileName(options.FileName, _today, i + 2), actualFiles2[i]);
            }
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

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
            var filePath1 = GetLogFilePath(path, options.FileName, _today, 0);
            var filePath2 = GetLogFilePath(path, options.FileName, _today, 1);
            var filePath3 = GetLogFilePath(path, options.FileName, _today, 2);

            await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageOne);
                logger.EnqueueMessage(_messageTwo);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(filePath2, _messageTwo.Length).DefaultTimeout();
            }

            // Even with a big enough FileSizeLimit, we still won't try to write to files from a previous instance.
            options.FileSizeLimit = 10000;

            await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageThree);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(filePath3, _messageThree.Length).DefaultTimeout();
            }

            var actualFiles = new DirectoryInfo(path)
                .GetFiles()
                .Select(f => f.Name)
                .OrderBy(f => f)
                .ToArray();

            Assert.Equal(3, actualFiles.Length);

            Assert.True(File.Exists(filePath1));
            Assert.True(File.Exists(filePath2));
            Assert.True(File.Exists(filePath3));

            Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(filePath1));
            Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(filePath2));
            Assert.Equal(_messageThree + Environment.NewLine, File.ReadAllText(filePath3));
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
            var filePath1 = GetLogFilePath(path, options.FileName, _today, 0);
            var filePath2 = GetLogFilePath(path, options.FileName, _today, 1);
            var filePath3 = GetLogFilePath(path, options.FileName, _today, 2);
            var filePath4 = GetLogFilePath(path, options.FileName, _today, 3);

            await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageOne);
                logger.EnqueueMessage(_messageTwo);
                logger.EnqueueMessage(_messageThree);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(filePath3, _messageThree.Length).DefaultTimeout();
            }

            // Even with a big enough FileSizeLimit, we still won't try to write to files from a previous instance.
            options.FileSizeLimit = 10000;

            await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageFour);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(filePath4, _messageFour.Length).DefaultTimeout();
            }

            var actualFiles = new DirectoryInfo(path)
                .GetFiles()
                .Select(f => f.Name)
                .OrderBy(f => f)
                .ToArray();

            Assert.Equal(2, actualFiles.Length);

            Assert.False(File.Exists(filePath1));
            Assert.False(File.Exists(filePath2));
            Assert.True(File.Exists(filePath3));
            Assert.True(File.Exists(filePath4));

            Assert.Equal(_messageThree + Environment.NewLine, File.ReadAllText(filePath3));
            Assert.Equal(_messageFour + Environment.NewLine, File.ReadAllText(filePath4));
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task WritesToNewFileOnOptionsChange(bool fieldsChanged, bool headersChanged)
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
                FileSizeLimit = 10000,
            };
            options.AdditionalRequestHeaders.Add("one");
            var filePath1 = GetLogFilePath(path, options.FileName, _today, 0);
            var filePath2 = GetLogFilePath(path, options.FileName, _today, 1);
            var monitor = new OptionsWrapperMonitor<W3CLoggerOptions>(options);

            await using (var logger = new FileLoggerProcessor(monitor, new HostingEnvironment(), NullLoggerFactory.Instance))
            {
                logger.SystemDateTime = mockSystemDateTime;
                logger.EnqueueMessage(_messageOne);
                await WaitForFile(filePath1, _messageOne.Length).DefaultTimeout();

                if (fieldsChanged)
                {
                    options.LoggingFields = W3CLoggingFields.Date;
                }

                if (headersChanged)
                {
                    options.AdditionalRequestHeaders.Remove("one");
                    options.AdditionalRequestHeaders.Add("two");
                }
                monitor.InvokeChanged();
                logger.EnqueueMessage(_messageTwo);
                // Pause for a bit before disposing so logger can finish logging
                await WaitForFile(filePath2, _messageTwo.Length).DefaultTimeout();
            }

            var actualFiles = new DirectoryInfo(path)
                .GetFiles()
                .Select(f => f.Name)
                .OrderBy(f => f)
                .ToArray();

            Assert.Equal(2, actualFiles.Length);

            Assert.True(File.Exists(filePath1));
            Assert.True(File.Exists(filePath2));

            Assert.Equal(_messageOne + Environment.NewLine, File.ReadAllText(filePath1));
            Assert.Equal(_messageTwo + Environment.NewLine, File.ReadAllText(filePath2));
        }
        finally
        {
            Helpers.DisposeDirectory(path);
        }
    }

    private async Task WaitForFile(string filePath, int length)
    {
        while (!File.Exists(filePath))
        {
            await Task.Delay(100);
        }
        while (true)
        {
            try
            {
                if (File.ReadAllText(filePath).Length >= length)
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

    private async Task WaitForRoll(string filePath)
    {
        while (File.Exists(filePath))
        {
            await Task.Delay(100);
        }
    }

    private static string GetLogFilePath(string path, string prefix, DateTime dateTime, int fileNumber)
    {
        return Path.Combine(path, GetLogFileName(prefix, dateTime, fileNumber));
    }

    private static string GetLogFileName(string prefix, DateTime dateTime, int fileNumber)
    {
        return FormattableString.Invariant($"{GetLogFileBaseName(prefix, dateTime)}.{fileNumber:0000}.txt");
    }

    private static string GetLogFileBaseName(string prefix, DateTime dateTime)
    {
        return FormattableString.Invariant($"{prefix}{dateTime.Year:0000}{dateTime.Month:00}{dateTime.Day:00}");
    }
}
