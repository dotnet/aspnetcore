// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.FunctionalTests
{
    public class DotNetWatcherTests : IDisposable
    {
        private readonly KitchenSinkApp _app;

        public DotNetWatcherTests(ITestOutputHelper logger)
        {
            _app = new KitchenSinkApp(logger);
        }

        [Fact]
        public async Task RunsWithDotnetWatchEnvVariable()
        {
            Assert.True(string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_WATCH")), "DOTNET_WATCH cannot be set already when this test is running");

            await _app.StartWatcherAsync();
            const string messagePrefix = "DOTNET_WATCH = ";
            var message = await _app.Process.GetOutputLineStartsWithAsync(messagePrefix, TimeSpan.FromMinutes(2));
            var envValue = message.Substring(messagePrefix.Length);
            Assert.Equal("1", envValue);
        }

        public void Dispose()
        {
            _app.Dispose();
        }

        private class KitchenSinkApp : WatchableApp
        {
            public KitchenSinkApp(ITestOutputHelper logger)
                : base("KitchenSink", logger)
            {
            }
        }
    }
}
