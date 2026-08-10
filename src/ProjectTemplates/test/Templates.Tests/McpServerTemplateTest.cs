// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit.Abstractions;

namespace Templates.Test;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class McpServerTemplateTest : LoggedTest
{
    public McpServerTemplateTest(ProjectFactoryFixture projectFactory)
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
    [SkipOnHelix("Self-contained template requires runtime packages unavailable in CI")]
    public async Task McpServerTemplate_Local()
    {
        await McpServerTemplateCoreAsync("local");
    }

    [ConditionalFact]
    [SkipOnHelix("Self-contained template requires runtime packages unavailable in CI")]
    public async Task McpServerTemplate_Remote()
    {
        await McpServerTemplateCoreAsync("remote");
    }

    [ConditionalFact]
    public async Task McpServerTemplate_Local_SelfContainedFalse()
    {
        await McpServerTemplateCoreAsync("local", args: ["--self-contained", "false"]);
    }

    [ConditionalFact]
    public async Task McpServerTemplate_Remote_SelfContainedFalse()
    {
        await McpServerTemplateCoreAsync("remote", args: ["--self-contained", "false"]);
    }

    [ConditionalFact]
    [SkipOnHelix("NativeAOT template requires runtime packages unavailable in CI")]
    public async Task McpServerTemplate_Local_NativeAot()
    {
        await McpServerTemplateCoreAsync("local", args: [ArgConstants.PublishNativeAot]);
    }

    [ConditionalFact]
    [SkipOnHelix("NativeAOT template requires runtime packages unavailable in CI")]
    public async Task McpServerTemplate_Remote_NativeAot()
    {
        await McpServerTemplateCoreAsync("remote", args: [ArgConstants.PublishNativeAot]);
    }

    private async Task McpServerTemplateCoreAsync(string transport, string[] args = null)
    {
        var nativeAot = args?.Contains(ArgConstants.PublishNativeAot) ?? false;

        var project = await ProjectFactory.CreateProject(Output);
        if (nativeAot)
        {
            project.SetCurrentRuntimeIdentifier();
        }

        var allArgs = new List<string> { "--transport", transport };
        if (args is not null)
        {
            allArgs.AddRange(args);
        }

        await project.RunDotNetNewAsync("mcpserver", args: allArgs.ToArray());

        if (transport == "remote")
        {
            var expectedLaunchProfileNames = new[] { "http", "https" };
            await project.VerifyLaunchSettings(expectedLaunchProfileNames);
        }

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

        if (transport == "local")
        {
            using (var aspNetProcess = project.StartBuiltProjectAsync(hasListeningUri: false))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));
            }

            using (var aspNetProcess = project.StartPublishedProjectAsync(hasListeningUri: false, usePublishedAppHost: nativeAot))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));
            }
        }
        else
        {
            using (var aspNetProcess = project.StartBuiltProjectAsync(hasListeningUri: true))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));
            }

            using (var aspNetProcess = project.StartPublishedProjectAsync(hasListeningUri: true, usePublishedAppHost: nativeAot))
            {
                Assert.False(
                    aspNetProcess.Process.HasExited,
                    ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));
            }
        }
    }
}
