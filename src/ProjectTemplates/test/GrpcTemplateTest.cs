// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
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

        [ConditionalFact(Skip = "This test run for over an hour")]
        [SkipOnHelix("Not supported queues", Queues = "Windows.7.Amd64;Windows.7.Amd64.Open;OSX.1014.Amd64;OSX.1014.Amd64.Open")]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/19716")]
        public async Task GrpcTemplate()
        {
            Project = await ProjectFactory.GetOrCreateProject("grpc", Output);

            var createResult = await Project.RunDotNetNewAsync("grpc");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", Project, createResult));

            var publishResult = await Project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", Project, publishResult));

            var buildResult = await Project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", Project, buildResult));

            using (var serverProcess = Project.StartBuiltProjectAsync())
            {
                // These templates are HTTPS + HTTP/2 only which is not supported on Mac due to missing ALPN support.
                // https://github.com/dotnet/aspnetcore/issues/11061
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Assert.True(serverProcess.Process.HasExited, "built");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on macOS due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", Project, serverProcess.Process));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version < new Version(6, 2))
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Assert.True(aspNetProcess.Process.HasExited, "published");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on macOS due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", Project, aspNetProcess.Process));
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version < new Version(6, 2))
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
