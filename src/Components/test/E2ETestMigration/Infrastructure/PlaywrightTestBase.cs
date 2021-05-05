// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using PlaywrightSharp;
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

        protected async override Task InitializeCoreAsync(TestContext context)
        {
            BrowserManager = await BrowserManager.CreateAsync(CreateConfiguration(), LoggerFactory);
            BrowserContextInfo = new ContextInformation(LoggerFactory);
            _output = new TestOutputLogger(Logger);
        }

        public Task InitializeAsync() => Task.CompletedTask;

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

        private ITestOutputHelper _output;
        public ITestOutputHelper Output
        {
            get
            {
                if (_output == null)
                {
                    _output = new TestOutputLogger(Logger);
                }
                return _output;
            }
        }

        public ContextInformation BrowserContextInfo { get; protected set; }
        public BrowserManager BrowserManager { get; private set; }

        protected async Task MountTestComponentAsync<TComponent>(IPage page)
        {
            var componentType = typeof(TComponent);
            var componentTypeName = componentType.Assembly == typeof(BasicTestApp.Program).Assembly ?
                componentType.FullName :
                componentType.AssemblyQualifiedName;
            var testSelector = await page.WaitForSelectorAsync("#test-selector > select");

            Output.WriteLine("Selecting test: " + componentTypeName);

            var option = $"#test-selector > select > option[value='{componentTypeName}']";
            var selected = await page.SelectOptionAsync("#test-selector > select", componentTypeName);
            Assert.True(selected.Length == 1);
            Assert.Equal(componentTypeName, selected.First());
        }

    }
}
