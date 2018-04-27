// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Tools
{
    public class ServerCommandTest
    {
        [Fact(Skip = "https://github.com/aspnet/Razor/issues/2310")]
        public void WritePidFile_WorksAsExpected()
        {
            // Arrange
            var expectedProcessId = Process.GetCurrentProcess().Id;
            var expectedRzcPath = typeof(ServerCommand).Assembly.Location;
            var expectedFileName = $"rzc-{expectedProcessId}";
            var homeEnvVariable = PlatformInformation.IsWindows ? "USERPROFILE" : "HOME";
            var path = Path.Combine(Environment.GetEnvironmentVariable(homeEnvVariable), ".dotnet", "pids", "build", expectedFileName);

            var pipeName = Guid.NewGuid().ToString();
            var server = GetServerCommand(pipeName);

            // Act & Assert
            try
            {
                using (var _ = server.WritePidFile())
                {
                    Assert.True(File.Exists(path));

                    // Make sure another stream can be opened while the write stream is still open.
                    using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Write | FileShare.Delete))
                    using (var reader = new StreamReader(fileStream))
                    {
                        var lines = reader.ReadToEnd().Split(Environment.NewLine);
                        Assert.Equal(new[] { expectedProcessId.ToString(), "rzc", expectedRzcPath, pipeName }, lines);
                    }
                }

                // Make sure the file is deleted on dispose.
                Assert.False(File.Exists(path));
            }
            finally
            {
                // Delete the file in case the test fails.
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        private ServerCommand GetServerCommand(string pipeName)
        {
            var application = new Application(
                CancellationToken.None,
                Mock.Of<ExtensionAssemblyLoader>(),
                Mock.Of<ExtensionDependencyChecker>(),
                (path, properties) => MetadataReference.CreateFromFile(path, properties));

            return new ServerCommand(application, pipeName);
        }
    }
}
