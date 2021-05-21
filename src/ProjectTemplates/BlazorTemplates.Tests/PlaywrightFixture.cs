// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ProjectTemplates.Tests.Infrastructure
{
    public class PlaywrightFixture<TTestAssemblyType> : IAsyncLifetime
    {
        private static readonly bool _isCIEnvironment =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ContinuousIntegrationBuild")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Helix"));

        private readonly IMessageSink _diagnosticsMessageSink;
        private static BrowserManagerConfiguration _config = new BrowserManagerConfiguration(CreateConfiguration(typeof(TTestAssemblyType).Assembly));

        public PlaywrightFixture(IMessageSink diagnosticsMessageSink)
        {
            _diagnosticsMessageSink = diagnosticsMessageSink;
        }

        private static IConfiguration CreateConfiguration(Assembly assembly)
        {
            var basePath = Path.GetDirectoryName(assembly.Location);
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

        public async Task InitializeAsync()
        {
            var sink = new TestSink();
            sink.MessageLogged += LogBrowserManagerMessage;
            var factory = new TestLoggerFactory(sink, enabled: true);
            BrowserManager = await BrowserManager.CreateAsync(_config, factory);
        }

        private void LogBrowserManagerMessage(WriteContext context)
        {
            _diagnosticsMessageSink.OnMessage(new DiagnosticMessage(context.Message));
        }

        public async Task DisposeAsync()
        {
            await BrowserManager.DisposeAsync();
        }

        public BrowserManager BrowserManager { get; set; }
    }
}
