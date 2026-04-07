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

    [ConditionalTheory]
    [InlineData("local", null)]
    [InlineData("remote", null)]
    public async Task McpServerTemplate_CanCreateBuildPublish(string transport, string[] args)
    {
        await McpServerTemplateCoreAsync(transport, args);
    }

    [ConditionalTheory]
    [InlineData("local")]
    [InlineData("remote")]
    public async Task McpServerTemplate_SelfContainedFalse(string transport)
    {
        await McpServerTemplateCoreAsync(transport, args: new[] { "--SelfContained", "false" });
    }

    [ConditionalTheory(Skip = "Unskip when Helix supports native AOT. https://github.com/dotnet/aspnetcore/pull/47247/")]
    [InlineData("local")]
    [InlineData("remote")]
    public async Task McpServerTemplate_NativeAot(string transport)
    {
        await McpServerTemplateCoreAsync(transport, args: new[] { ArgConstants.PublishNativeAot });
    }

    private async Task McpServerTemplateCoreAsync(string transport, string[] args)
    {
        var nativeAot = args?.Contains(ArgConstants.PublishNativeAot) ?? false;

        var project = await ProjectFactory.CreateProject(Output);
        if (nativeAot)
        {
            project.SetCurrentRuntimeIdentifier();
        }

        var allArgs = new List<string> { "--transport", transport };
        if (args != null)
        {
            allArgs.AddRange(args);
        }

        await project.RunDotNetNewAsync("mcpserver", args: allArgs.ToArray());

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
    }
}
