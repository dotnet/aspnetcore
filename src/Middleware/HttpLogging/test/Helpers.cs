// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpLogging;

internal static class Helpers
{
    public static void DisposeDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
            // ignored
        }
    }

    public static TestW3CLogger CreateTestW3CLogger(IOptionsMonitor<W3CLoggerOptions> options)
    {
        return new TestW3CLogger(
            options,
            new TestW3CLoggerProcessor(
                options,
                new HostingEnvironment(),
                NullLoggerFactory.Instance));
    }

    public static string GetLogFilePath(string path, string prefix, DateTime dateTime, int fileNumber)
    {
        return Path.Combine(path, GetLogFileName(prefix, dateTime, fileNumber));
    }

    public static string GetLogFileName(string prefix, DateTime dateTime, int fileNumber)
    {
        return FormattableString.Invariant($"{GetLogFileBaseName(prefix, dateTime)}.{fileNumber:0000}.txt");
    }

    public static string GetLogFileBaseName(string prefix, DateTime dateTime)
    {
        return FormattableString.Invariant($"{prefix}{dateTime.Year:0000}{dateTime.Month:00}{dateTime.Day:00}");
    }
}
