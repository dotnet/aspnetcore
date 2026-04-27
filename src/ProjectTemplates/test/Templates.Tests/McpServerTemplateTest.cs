// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
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
    public async Task McpServerTemplate_Local()
    {
        await McpServerTemplateCoreAsync("local");
    }

    [ConditionalFact]
    public async Task McpServerTemplate_Remote()
    {
        await McpServerTemplateCoreAsync("remote");
    }

    [ConditionalFact]
    public async Task McpServerTemplate_Local_SelfContainedFalse()
    {
        await McpServerTemplateCoreAsync("local", args: new[] { "--self-contained", "false" });
    }

    [ConditionalFact]
    public async Task McpServerTemplate_Remote_SelfContainedFalse()
    {
        await McpServerTemplateCoreAsync("remote", args: new[] { "--self-contained", "false" });
    }

    [ConditionalFact]
    [SkipOnHelix("Not supported queues", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task McpServerTemplate_Local_NativeAot()
    {
        await McpServerTemplateCoreAsync("local", args: new[] { ArgConstants.PublishNativeAot });
    }

    [ConditionalFact]
    [SkipOnHelix("Not supported queues", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task McpServerTemplate_Remote_NativeAot()
    {
        await McpServerTemplateCoreAsync("remote", args: new[] { ArgConstants.PublishNativeAot });
    }

    private async Task McpServerTemplateCoreAsync(string transport, string[] args = null)
    {
        var nativeAot = args?.Contains(ArgConstants.PublishNativeAot) ?? false;
        var selfContainedFalse = args is not null
            && args.Contains("--self-contained")
            && args.Contains("false");
        var needsSingleRid = !selfContainedFalse;

        var project = await ProjectFactory.CreateProject(Output);
        if (needsSingleRid)
        {
            project.SetCurrentRuntimeIdentifier();
        }

        var allArgs = new List<string> { "--transport", transport };
        if (args != null)
        {
            allArgs.AddRange(args);
        }

        // The default template uses SelfContained=true with multi-RID RuntimeIdentifiers.
        // CI feeds don't have all RID-specific runtime packages, so replace the multi-RID
        // property with a single RuntimeIdentifier for the current platform before restore.
        Action preRestoreAction = needsSingleRid
            ? () => ReplaceRuntimeIdentifiersWithSingle(project)
            : null;

        await project.RunDotNetNewAsync("mcpserver", args: allArgs.ToArray(), preRestoreAction: preRestoreAction);

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

    /// <summary>
    /// Replaces multi-RID <c>&lt;RuntimeIdentifiers&gt;</c> with a single
    /// <c>&lt;RuntimeIdentifier&gt;</c> matching the current platform so that
    /// NuGet restore only needs the current platform's runtime packages.
    /// </summary>
    private static void ReplaceRuntimeIdentifiersWithSingle(Project project)
    {
        var csprojPath = Directory.GetFiles(project.TemplateOutputDir, "*.csproj").Single();
        var content = File.ReadAllText(csprojPath);
        content = Regex.Replace(
            content,
            @"<RuntimeIdentifiers>[^<]+</RuntimeIdentifiers>",
            $"<RuntimeIdentifier>{project.RuntimeIdentifier}</RuntimeIdentifier>");
        File.WriteAllText(csprojPath, content);
    }
}
