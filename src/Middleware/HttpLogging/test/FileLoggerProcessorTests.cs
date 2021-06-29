// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.HttpLogging
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
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path
                };
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.EnqueueMessage("Message one");
                    fileName = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0000.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    try
                    {
                        await WaitForFile(fileName).DefaultTimeout();
                    }
                    catch
                    {
                        // Midnight could have struck between taking the DateTime & writing the log
                        if (!File.Exists(fileName))
                        {
                            var tomorrow = now.AddDays(1);
                            fileName = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{tomorrow.Year:0000}{tomorrow.Month:00}{tomorrow.Day:00}.0000.txt"));
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
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    FileSizeLimit = 5
                };
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.EnqueueMessage("Message one");
                    logger.EnqueueMessage("Message two");
                    fileName1 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0000.txt"));
                    fileName2 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0001.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    try
                    {
                        await WaitForFile(fileName2).DefaultTimeout();
                    }
                    catch
                    {
                        // Midnight could have struck between taking the DateTime & writing the log
                        // It also could have struck between writing file 1 & file 2
                        var tomorrow = now.AddDays(1);
                        if (!File.Exists(fileName1))
                        {
                            fileName1 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{tomorrow.Year:0000}{tomorrow.Month:00}{tomorrow.Day:00}.0000.txt"));
                            fileName2 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{tomorrow.Year:0000}{tomorrow.Month:00}{tomorrow.Day:00}.0001.txt"));
                        }
                        else if (!File.Exists(fileName2))
                        {
                            fileName2 = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{tomorrow.Year:0000}{tomorrow.Month:00}{tomorrow.Day:00}.0000.txt"));
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
                string lastFileName;
                var now = DateTimeOffset.Now;
                var tomorrow = now.AddDays(1);
                var options = new W3CLoggerOptions()
                {
                    LogDirectory = path,
                    RetainedFileCountLimit = 3,
                    FileSizeLimit = 5
                };
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    for (int i = 0; i < 10; i++)
                    {
                        logger.EnqueueMessage("Message");
                    }
                    lastFileName = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0009.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    try
                    {
                        await WaitForFile(lastFileName).DefaultTimeout();
                        for (int i = 0; i < 6; i++)
                        {
                            await WaitForRoll(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.{i:0000}.txt"))).DefaultTimeout();
                        }
                    }
                    catch
                    {
                        // Midnight could have struck between taking the DateTime & writing the log.
                        // It also could have struck any time after writing the first file.
                        // So we keep going even if waiting timed out, in case we're wrong about the assumed file name
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
                    // File name will either start with today's date or tomorrow's date (if midnight struck during the execution of the test)
                    Assert.True((actualFiles[i].StartsWith($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}", StringComparison.InvariantCulture)) ||
                        (actualFiles[i].StartsWith($"{options.FileName}{tomorrow.Year:0000}{tomorrow.Month:00}{tomorrow.Day:00}", StringComparison.InvariantCulture)));
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
            var now = DateTimeOffset.Now;
            if (now.Hour == 23)
            {
                // Don't bother trying to run this test when it's almost midnight.
                return;
            }

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
                    for (int i = 0; i < 3; i++)
                    {
                        logger.EnqueueMessage("Message");
                    }
                    var filePath = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0002.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(filePath).DefaultTimeout();
                }

                // Second instance should pick up where first one left off
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        logger.EnqueueMessage("Message");
                    }
                    var filePath = Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0005.txt"));
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(filePath).DefaultTimeout();
                }

                var actualFiles1 = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(6, actualFiles1.Length);
                for (int i = 0; i < 6; i++)
                {
                    Assert.Contains($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.{i:0000}.txt", actualFiles1[i]);
                }

                // Third instance should roll to 5 most recent files
                options.RetainedFileCountLimit = 5;
                await using (var logger = new FileLoggerProcessor(new OptionsWrapperMonitor<W3CLoggerOptions>(options), new HostingEnvironment(), NullLoggerFactory.Instance))
                {
                    logger.EnqueueMessage("Message");
                    // Pause for a bit before disposing so logger can finish logging
                    await WaitForFile(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0006.txt"))).DefaultTimeout();
                    await WaitForRoll(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0000.txt"))).DefaultTimeout();
                    await WaitForRoll(Path.Combine(path, FormattableString.Invariant($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.0001.txt"))).DefaultTimeout();
                }

                var actualFiles2 = new DirectoryInfo(path)
                    .GetFiles()
                    .Select(f => f.Name)
                    .OrderBy(f => f)
                    .ToArray();

                Assert.Equal(5, actualFiles2.Length);
                for (int i = 0; i < 5; i++)
                {
                    Assert.Equal($"{options.FileName}{now.Year:0000}{now.Month:00}{now.Day:00}.{i + 2:0000}.txt", actualFiles2[i]);
                }
            }
            finally
            {
                Helpers.DisposeDirectory(path);
            }
        }

        private async Task WaitForFile(string fileName)
        {
            while (!File.Exists(fileName))
            {
                await Task.Delay(100);
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
