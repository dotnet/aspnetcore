// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Extensions.Logging.AzureAppServices.Test;

public class FileLoggerTests : IDisposable
{
    DateTimeOffset _timestampOne = new DateTimeOffset(2016, 05, 04, 03, 02, 01, TimeSpan.Zero);

    public FileLoggerTests()
    {
        TempPath = Path.GetTempFileName() + "_";
    }

    public string TempPath { get; }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(TempPath))
            {
                Directory.Delete(TempPath, true);
            }
        }
        catch
        {
            // ignored
        }
    }

    [Fact]
    public async Task WritesToTextFile()
    {
        var provider = new TestFileLoggerProvider(TempPath);
        var logger = (BatchingLogger)provider.CreateLogger("Cat");

        await provider.IntervalControl.Pause;

        logger.Log(_timestampOne, LogLevel.Information, 0, "Info message", null, (state, ex) => state);
        logger.Log(_timestampOne.AddHours(1), LogLevel.Error, 0, "Error message", null, (state, ex) => state);

        provider.IntervalControl.Resume();
        await provider.IntervalControl.Pause;

        Assert.Equal(
            "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Info message" + Environment.NewLine +
            "2016-05-04 04:02:01.000 +00:00 [Error] Cat: Error message" + Environment.NewLine,
            File.ReadAllText(Path.Combine(TempPath, "LogFile.20160504.txt")));
    }

    [Fact]
    public async Task RollsTextFile()
    {
        var provider = new TestFileLoggerProvider(TempPath);
        var logger = (BatchingLogger)provider.CreateLogger("Cat");

        await provider.IntervalControl.Pause;

        logger.Log(_timestampOne, LogLevel.Information, 0, "Info message", null, (state, ex) => state);
        logger.Log(_timestampOne.AddDays(1), LogLevel.Error, 0, "Error message", null, (state, ex) => state);

        provider.IntervalControl.Resume();
        await provider.IntervalControl.Pause;

        Assert.Equal(
            "2016-05-04 03:02:01.000 +00:00 [Information] Cat: Info message" + Environment.NewLine,
            File.ReadAllText(Path.Combine(TempPath, "LogFile.20160504.txt")));

        Assert.Equal(
            "2016-05-05 03:02:01.000 +00:00 [Error] Cat: Error message" + Environment.NewLine,
            File.ReadAllText(Path.Combine(TempPath, "LogFile.20160505.txt")));
    }

    [Fact]
    public async Task RespectsMaxFileCount()
    {
        Directory.CreateDirectory(TempPath);
        File.WriteAllText(Path.Combine(TempPath, "randomFile.txt"), "Text");

        var provider = new TestFileLoggerProvider(TempPath, maxRetainedFiles: 5);
        var logger = (BatchingLogger)provider.CreateLogger("Cat");

        await provider.IntervalControl.Pause;
        var timestamp = _timestampOne;

        for (int i = 0; i < 10; i++)
        {
            logger.Log(timestamp, LogLevel.Information, 0, "Info message", null, (state, ex) => state);
            logger.Log(timestamp.AddHours(1), LogLevel.Error, 0, "Error message", null, (state, ex) => state);

            timestamp = timestamp.AddDays(1);
        }

        provider.IntervalControl.Resume();
        await provider.IntervalControl.Pause;

        var actualFiles = new DirectoryInfo(TempPath)
            .GetFiles()
            .Select(f => f.Name)
            .OrderBy(f => f)
            .ToArray();

        Assert.Equal(6, actualFiles.Length);
        Assert.Equal(new[] {
                "LogFile.20160509.txt",
                "LogFile.20160510.txt",
                "LogFile.20160511.txt",
                "LogFile.20160512.txt",
                "LogFile.20160513.txt",
                "randomFile.txt"
            }, actualFiles);
    }
}
