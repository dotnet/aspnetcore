using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging.Tests
{
    public class FileLoggerProcessorTests
    {
        public FileLoggerProcessorTests()
        {
            TempPath = Path.GetTempFileName() + "_";
        }

        public string TempPath { get; }

        [Fact]
        public async Task WritesToTextFile()
        {
            var path = Path.Combine(TempPath, Path.GetRandomFileName());

            try
            {
                string fileName;
                var now = DateTimeOffset.Now;
                var options = new FileLoggerOptions()
                {
                    LogDirectory = path
                };
                using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<FileLoggerOptions>(options)))
                {
                    logger.EnqueueMessage(new LogMessage(now, "Message one"));
                    fileName = Path.Combine(path, $"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}01.txt");
                    // Pause for a bit before disposing so logger can finish logging
                    for (int i = 0; i < 50; i++)
                    {
                        if (File.Exists(fileName))
                        {
                            break;
                        }
                        else
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                Assert.True(File.Exists(fileName));

                Assert.Equal("Message one" + Environment.NewLine, File.ReadAllText(fileName));
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        [Fact]
        public async Task RollsTextFiles()
        {
            var path = Path.Combine(TempPath, Path.GetRandomFileName());

            try
            {
                string fileName1;
                string fileName2;
                var now = DateTimeOffset.Now;
                var options = new FileLoggerOptions()
                {
                    LogDirectory = path,
                    FileSizeLimit = 5
                };
                using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<FileLoggerOptions>(options)))
                {
                    logger.EnqueueMessage(new LogMessage(now, "Message one"));
                    logger.EnqueueMessage(new LogMessage(now, "Message two"));
                    fileName1 = Path.Combine(path, $"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}01.txt");
                    fileName2 = Path.Combine(path, $"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}02.txt");
                    // Pause for a bit before disposing so logger can finish logging
                    for (int i = 0; i < 50; i++)
                    {
                        if (File.Exists(fileName2))
                        {
                            break;
                        }
                        else
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                Assert.True(File.Exists(fileName1));
                Assert.True(File.Exists(fileName2));

                Assert.Equal("Message one" + Environment.NewLine, File.ReadAllText(fileName1));
                Assert.Equal("Message two" + Environment.NewLine, File.ReadAllText(fileName2));
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

            try
            {
                string fileName;
                var timestamp = new DateTimeOffset(2020, 01, 02, 03, 04, 05, TimeSpan.Zero);
                var options = new FileLoggerOptions()
                {
                    LogDirectory = path,
                    RetainedFileCountLimit = 3
                };
                using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<FileLoggerOptions>(options)))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        timestamp = timestamp.AddDays(1);
                        logger.EnqueueMessage(new LogMessage(timestamp, "Message"));
                    }
                    fileName = Path.Combine(path, $"{options.FileName}{timestamp.Year:0000}{timestamp.Month:00}{timestamp.Day:00}01.txt");
                    // Pause for a bit before disposing so logger can finish logging
                    for (int i = 0; i < 50; i++)
                    {
                        if (File.Exists(fileName))
                        {
                            break;
                        }
                        else
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                Assert.True(File.Exists(fileName));

                var actualFiles = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(4, actualFiles.Length);
                Assert.Equal(new[] {
                "log-2020011001.txt",
                "log-2020011101.txt",
                "log-2020011201.txt",
                "randomFile.txt"
            }, actualFiles);
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }
    }
}
