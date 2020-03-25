// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test
{
    public class GrpcTemplateTest
    {
        public GrpcTemplateTest(ProjectFactoryFixture projectFactory, ITestOutputHelper output)
        {
            ProjectFactory = projectFactory;
            Output = output;
        }

        public Project Project { get; set; }

        public ProjectFactoryFixture ProjectFactory { get; }
        public ITestOutputHelper Output { get; }

        [ConditionalFact]
        [SkipOnHelix("Not supported queues", Queues = "Windows.7.Amd64;Windows.7.Amd64.Open;OSX.1014.Amd64;OSX.1014.Amd64.Open")]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19716")]
        public async Task GrpcTemplate()
        {
            // Setup AssemblyTestLog
            var assemblyLog = AssemblyTestLog.Create(Assembly.GetExecutingAssembly(), baseDirectory: Project.ArtifactsLogDir);
            using var testLog = assemblyLog.StartTestLog(Output, nameof(GrpcTemplateTest), out var loggerFactory);
            var logger = loggerFactory.CreateLogger("TestLogger");

            Project = await ProjectFactory.GetOrCreateProject("grpc", Output);

            var createResult = await Project.RunDotNetNewAsync("grpc");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            var isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            var isWindowsOld = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version < new Version(6, 2);
            var unsupported = isOsx || isWindowsOld;

            using (var serverProcess = Project.StartBuiltProjectAsync(logger: logger))
            {
                logger.LogInformation($"GrpcTemplateTest.cs - isOsx: {isOsx} unsupported: {unsupported} Process.HasExited: {serverProcess.Process.HasExited}");
                // These templates are HTTPS + HTTP/2 only which is not supported on Mac due to missing ALPN support.
                // https://github.com/dotnet/aspnetcore/issues/11061
                if (isOsx)
                {
                    Assert.True(serverProcess.Process.HasExited, "built");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on macOS due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", Project, serverProcess.Process));
                }
                else if (isWindowsOld)
                {
                    Assert.True(serverProcess.Process.HasExited, "built");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on Windows 7 due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", Project, serverProcess.Process));
                }
                else
                {
                    Assert.False(
                        serverProcess.Process.HasExited,
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", Project, serverProcess.Process));
                }
            }

            using (var aspNetProcess = Project.StartPublishedProjectAsync())
            {
                // These templates are HTTPS + HTTP/2 only which is not supported on Mac due to missing ALPN support.
                // https://github.com/dotnet/aspnetcore/issues/11061
                if (isOsx)
                {
                    Assert.True(aspNetProcess.Process.HasExited, "published");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on macOS due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", Project, aspNetProcess.Process));
                }
                else if (isWindowsOld)
                {
                    Assert.True(aspNetProcess.Process.HasExited, "published");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on Windows 7 due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", Project, aspNetProcess.Process));
                }
                else
                {
                    Assert.False(
                        aspNetProcess.Process.HasExited,
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", Project, aspNetProcess.Process));
                }
            }
        }
    }
}
