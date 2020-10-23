// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit.Abstractions;

namespace Templates.Test
{
    public static class TestHelpers
    {
        public static ILoggerFactory CreateFactory(ITestOutputHelper output)
        {
            var testSink = new TestSink();
            testSink.MessageLogged += LogMessage;
            var loggerFactory = new TestLoggerFactory(testSink, enabled: true);
            return loggerFactory;

            void LogMessage(WriteContext ctx)
            {
                output.WriteLine($"{MapLogLevel(ctx)}: [Browser]{ctx.Message}");

                static string MapLogLevel(WriteContext obj) => obj.LogLevel switch
                {
                    LogLevel.Trace => "trace",
                    LogLevel.Debug => "dbug",
                    LogLevel.Information => "info",
                    LogLevel.Warning => "warn",
                    LogLevel.Error => "error",
                    LogLevel.Critical => "crit",
                    LogLevel.None => "info",
                    _ => "info"
                };
            }
        }

        public static bool TryValidateBrowserRequired(BrowserKind browserKind, bool isRequired, out string error)
        {
            error = !isRequired ? null : $"Browser '{browserKind}' is required but not configured on '{RuntimeInformation.OSDescription}'";
            return isRequired;
        }
    }
}
