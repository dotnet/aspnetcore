// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using PlaywrightSharp;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.BrowserTesting
{
    public class BrowserTestBase : LoggedTest, IAsyncLifetime
    {
        private static readonly bool _isCIEnvironment =
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ContinuousIntegrationBuild")) ||
            !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Helix"));

        private static BrowserManagerConfiguration _config = new BrowserManagerConfiguration(CreateConfiguration());

        public BrowserTestBase(ITestOutputHelper output = null) : base(output) { }

        protected async override Task InitializeCoreAsync(TestContext context)
        {
            BrowserManager = await BrowserManager.CreateAsync(_config, LoggerFactory);
            BrowserContextInfo = new ContextInformation(LoggerFactory);
            _output = new BrowserTestOutputLogger(Logger);
        }

        public Task InitializeAsync() => Task.CompletedTask;

        private static IConfiguration CreateConfiguration()
        {
            var basePath = Path.GetDirectoryName(typeof(BrowserTestBase).Assembly.Location);
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

        public virtual Task DisposeAsync() => BrowserManager.DisposeAsync();

        private ITestOutputHelper _output;
        public ITestOutputHelper Output
        {
            get
            {
                if (_output == null)
                {
                    _output = new BrowserTestOutputLogger(Logger);
                }
                return _output;
            }
        }

        public ContextInformation BrowserContextInfo { get; protected set; }
        public BrowserManager BrowserManager { get; private set; }
    }
}
