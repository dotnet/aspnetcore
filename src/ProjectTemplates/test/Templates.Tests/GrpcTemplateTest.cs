// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
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

    [ConditionalFact]
    [SkipOnHelix("Not supported queues", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
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

    [ConditionalFact]
    [SkipOnHelix("Not supported queues", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    [SkipOnAlpine("https://github.com/grpc/grpc/issues/18338")] // protoc doesn't support Alpine. Note that the issue was closed with a workaround which isn't applied to our OS image.
    public async Task GrpcTemplateProgramMainNativeAot()
    {
        await GrpcTemplateCore(args: new[] { ArgConstants.UseProgramMain, ArgConstants.PublishNativeAot });
    }

    private async Task GrpcTemplateCore(string[] args = null)
    {
        var nativeAot = args?.Contains(ArgConstants.PublishNativeAot) ?? false;

        var project = await ProjectFactory.CreateProject(Output);
        if (nativeAot)
        {
            project.SetCurrentRuntimeIdentifier();
        }

        await project.RunDotNetNewAsync("grpc", args: args);

        var expectedLaunchProfileNames = new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        if (nativeAot)
        {
            await project.VerifyHasProperty("InvariantGlobalization", "true");
        }

        // Force a restore if native AOT so that RID-specific assets are restored
        await project.RunDotNetPublishAsync(noRestore: !nativeAot);

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

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

        using (var aspNetProcess = project.StartPublishedProjectAsync(hasListeningUri: !isWindowsOld, usePublishedAppHost: nativeAot))
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
