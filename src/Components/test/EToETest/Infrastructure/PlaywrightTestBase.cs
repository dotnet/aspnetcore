// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Components.E2ETest.Infrastructure
{
    public class PlaywrightTestBase : LoggedTest, IAsyncLifetime
    {
        private static readonly bool _isCIEnvironment =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ContinuousIntegrationBuild"));

        public PlaywrightTestBase(ITestOutputHelper output) : base(output) { }

        public async Task InitializeAsync()
        {
            var testSink = new TestSink();
            testSink.MessageLogged += LogMessage;
            var factory = new TestLoggerFactory(testSink, enabled: true);
            BrowserManager = await BrowserManager.CreateAsync(CreateConfiguration(), factory);
            BrowserContextInfo = new ContextInformation(factory);

            void LogMessage(WriteContext ctx)
            {
                TestOutputHelper.WriteLine($"{MapLogLevel(ctx)}: [Browser]{ctx.Message}");

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

        private static IConfiguration CreateConfiguration()
        {
            var basePath = Path.GetDirectoryName(typeof(PlaywrightTestBase).Assembly.Location);
            var os = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "win",
                PlatformID.Unix => "linux",
                PlatformID.MacOSX => "osx",
                _ => null
            };

            var builder = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(basePath, "playwrightSettings.json"))
                .AddJsonFile(Path.Combine(basePath, $"playwrightSettings.{os}.json"), optional: true);

            if (_isCIEnvironment)
            {
                builder.AddJsonFile(Path.Combine(basePath, "playwrightSettings.ci.json"), optional: true)
                    .AddJsonFile(Path.Combine(basePath, $"playwrightSettings.ci.{os}.json"), optional: true);
            }

            if (Debugger.IsAttached)
            {
                builder.AddJsonFile(Path.Combine(basePath, "playwrightSettings.debug.json"), optional: true);
            }

            return builder.Build();
        }

        public Task DisposeAsync() => BrowserManager.DisposeAsync();

        public ITestOutputHelper Output => TestOutputHelper;
        public ContextInformation BrowserContextInfo { get; protected set; }
        public BrowserManager BrowserManager { get; private set; }
    }
}
