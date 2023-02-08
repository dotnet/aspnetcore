// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

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
    [SkipOnHelix("Not supported queues", Queues = "windows.11.arm64.open;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [SkipOnAlpine("https://github.com/grpc/grpc/issues/18338")] // protoc doesn't support Alpine. Note that the issue was closed with a workaround which isn't applied to our OS image.
    public async Task GrpcTemplate()
    {
        await GrpcTemplateCore();
    }

    [ConditionalFact(Skip = "Unskip when there are no more build or publish warnings for native AOT.")]
    [SkipOnHelix("Not supported queues", Queues = "windows.11.arm64.open;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [SkipOnAlpine("https://github.com/grpc/grpc/issues/18338")] // protoc doesn't support Alpine. Note that the issue was closed with a workaround which isn't applied to our OS image.
    public async Task GrpcTemplateNativeAot()
    {
        await GrpcTemplateCore(args: new[] { ArgConstants.PublishNativeAot });
    }

    [ConditionalFact]
    [SkipOnHelix("Not supported queues", Queues = "windows.11.arm64.open;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [SkipOnAlpine("https://github.com/grpc/grpc/issues/18338")] // protoc doesn't support Alpine. Note that the issue was closed with a workaround which isn't applied to our OS image.
    public async Task GrpcTemplateProgramMain()
    {
        await GrpcTemplateCore(args: new[] { ArgConstants.UseProgramMain });
    }

    [ConditionalFact(Skip = "Unskip when there are no more build or publish warnings for native AOT.")]
    [SkipOnHelix("Not supported queues", Queues = "windows.11.arm64.open;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [SkipOnAlpine("https://github.com/grpc/grpc/issues/18338")] // protoc doesn't support Alpine. Note that the issue was closed with a workaround which isn't applied to our OS image.
    public async Task GrpcTemplateProgramMainNativeAot()
    {
        await GrpcTemplateCore(args: new[] { ArgConstants.UseProgramMain, ArgConstants.PublishNativeAot });
    }

    private async Task GrpcTemplateCore(string[] args = null)
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("grpc", args: args);

        var expectedLaunchProfileNames = new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        await project.RunDotNetPublishAsync();

        await project.RunDotNetBuildAsync();

        var isWindowsOld = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.OSVersion.Version < new Version(6, 2);

        using (var serverProcess = project.StartBuiltProjectAsync(hasListeningUri: !isWindowsOld, logger: Logger))
        {
            // These templates are HTTPS + HTTP/2 only which is not supported on some platforms.
            if (isWindowsOld)
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

        using (var aspNetProcess = project.StartPublishedProjectAsync(hasListeningUri: !isWindowsOld))
        {
            // These templates are HTTPS + HTTP/2 only which is not supported on some platforms.
            if (isWindowsOld)
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
