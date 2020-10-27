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
    public class GrpcTemplateTest : LoggedTest
    {
        public GrpcTemplateTest(ProjectFactoryFixture projectFactory)
        {
            ProjectFactory = projectFactory;
        }

        public ProjectFactoryFixture ProjectFactory { get; }
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

        [ConditionalFact]
        [SkipOnHelix("Not supported queues", Queues = "Windows.7.Amd64;Windows.7.Amd64.Open;Windows.81.Amd64.Open;All.OSX;All.Alpine")]
        public async Task GrpcTemplate()
        {
            var project = await ProjectFactory.GetOrCreateProject("grpc", Output);

            var createResult = await project.RunDotNetNewAsync("grpc");
            Assert.True(0 == createResult.ExitCode, ErrorMessages.GetFailedProcessMessage("create/restore", project, createResult));

            var publishResult = await project.RunDotNetPublishAsync();
            Assert.True(0 == publishResult.ExitCode, ErrorMessages.GetFailedProcessMessage("publish", project, publishResult));

            var buildResult = await project.RunDotNetBuildAsync();
            Assert.True(0 == buildResult.ExitCode, ErrorMessages.GetFailedProcessMessage("build", project, buildResult));

            var isOsx = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            var isWindowsOld = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version < new Version(6, 2);
            var unsupported = isOsx || isWindowsOld;

            using (var serverProcess = project.StartBuiltProjectAsync(hasListeningUri: !unsupported, logger: Logger))
            {
                // These templates are HTTPS + HTTP/2 only which is not supported on Mac due to missing ALPN support.
                // https://github.com/dotnet/aspnetcore/issues/11061
                if (isOsx)
                {
                    serverProcess.Process.WaitForExit(assertSuccess: false);
                    Assert.True(serverProcess.Process.HasExited, "built");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on macOS due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", project, serverProcess.Process));
                }
                else if (isWindowsOld)
                {
                    serverProcess.Process.WaitForExit(assertSuccess: false);
                    Assert.True(serverProcess.Process.HasExited, "built");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on Windows 7 due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", project, serverProcess.Process));
                }
                else
                {
                    Assert.False(
                        serverProcess.Process.HasExited,
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run built service", project, serverProcess.Process));
                }
            }

            using (var aspNetProcess = project.StartPublishedProjectAsync(hasListeningUri: !unsupported))
            {
                // These templates are HTTPS + HTTP/2 only which is not supported on Mac due to missing ALPN support.
                // https://github.com/dotnet/aspnetcore/issues/11061
                if (isOsx)
                {
                    aspNetProcess.Process.WaitForExit(assertSuccess: false);
                    Assert.True(aspNetProcess.Process.HasExited, "published");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on macOS due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", project, aspNetProcess.Process));
                }
                else if (isWindowsOld)
                {
                    aspNetProcess.Process.WaitForExit(assertSuccess: false);
                    Assert.True(aspNetProcess.Process.HasExited, "published");
                    Assert.Contains("System.NotSupportedException: HTTP/2 over TLS is not supported on Windows 7 due to missing ALPN support.",
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", project, aspNetProcess.Process));
                }
                else
                {
                    Assert.False(
                        aspNetProcess.Process.HasExited,
                        ErrorMessages.GetFailedProcessMessageOrEmpty("Run published service", project, aspNetProcess.Process));
                }
            }
        }
    }
}
