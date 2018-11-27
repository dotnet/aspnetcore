// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Razor.Tools;
using Microsoft.CodeAnalysis;
using Moq;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    public class BuildServerTestFixture : IDisposable
    {
        private static readonly TimeSpan _defaultShutdownTimeout = TimeSpan.FromSeconds(60);

        public BuildServerTestFixture() : this(Guid.NewGuid().ToString())
        {
        }

        internal BuildServerTestFixture(string pipeName)
        {
            PipeName = pipeName;

            if (!ServerConnection.TryCreateServerCore(Environment.CurrentDirectory, PipeName, out var processId))
            {
                throw new InvalidOperationException($"Failed to start the build server at pipe {PipeName}.");
            }

            ProcessId = processId;
        }

        public string PipeName { get; }

        public int? ProcessId { get; }

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

                var exitCode = application.Execute("shutdown", "-w", "-p", PipeName);
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
