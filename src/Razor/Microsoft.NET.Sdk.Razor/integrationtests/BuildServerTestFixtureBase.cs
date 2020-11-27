// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.AspNetCore.Razor.Tools;
using Microsoft.CodeAnalysis;
using Moq;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public abstract class BuildServerTestFixtureBase : IDisposable
    {
        private static readonly TimeSpan _defaultShutdownTimeout = TimeSpan.FromSeconds(120);

        protected BuildServerTestFixtureBase(string pipeName)
        {
            PipeName = pipeName;
        }

        public string PipeName { get; }

        public void Dispose()
        {
            // Shutdown the build server.
            using (var cts = new CancellationTokenSource(_defaultShutdownTimeout))
            {
                var writer = new StringWriter();

                cts.Token.Register(() =>
                {
                    var output = writer.ToString();
                    throw new TimeoutException($"Shutting down the build server at pipe {PipeName} took longer than expected.{Environment.NewLine}Output: {output}.");
                });

                var application = new Application(cts.Token, Mock.Of<ExtensionAssemblyLoader>(), Mock.Of<ExtensionDependencyChecker>(), (path, properties) => Mock.Of<PortableExecutableReference>(), writer, writer);

                var args = new List<string>
                {
                    "shutdown",
                    "-p",
                    PipeName,
                };

                if (!OperatingSystem.IsLinux())
                {
                    // Workaround for https://github.com/dotnet/corefx/issues/31713. On Linux, the server shuts down but hangs around as defunct process
                    // We'll send a shutdown request but not wait for it.
                    args.Add("-w");
                }

                var exitCode = application.Execute(args.ToArray());
                if (exitCode != 0)
                {
                    var output = writer.ToString();
                    throw new InvalidOperationException(
                        $"Build server at pipe {PipeName} failed to shutdown with exit code {exitCode}. Output: {output}");
                }
            }
        }
    }
}
