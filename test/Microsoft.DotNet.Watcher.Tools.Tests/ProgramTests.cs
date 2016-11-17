// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Tools.Internal;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.DotNet.Watcher.Tools.Tests
{
    public class ProgramTests : IDisposable
    {
        private readonly TemporaryDirectory _tempDir;
        private readonly TestConsole _console;

        public ProgramTests(ITestOutputHelper output)
        {
            _tempDir = new TemporaryDirectory();
            _console = new TestConsole(output);
        }

        [Fact]
        public async Task ConsoleCancelKey()
        {
            _tempDir
                .WithCSharpProject("testproj")
                .WithTargetFrameworks("netcoreapp1.0")
                .Dir()
                .WithFile("Program.cs")
                .Create();

            var stdout = new StringBuilder();
            _console.Out = new StringWriter(stdout);
            var program = new Program(_console, _tempDir.Root)
                .RunAsync(new [] { "run" });

            _console.ConsoleCancelKey();

            var exitCode = await program.OrTimeout();

            Assert.Contains("Shutdown requested. Press Ctrl+C again to force exit.", stdout.ToString());
            Assert.Equal(0, exitCode);
        }
        public void Dispose()
        {
            _tempDir.Dispose();
        }
    }
}