// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit.Abstractions;

namespace Templates.Test;

public class WorkerTemplateTest : LoggedTest
{
    public WorkerTemplateTest(ProjectFactoryFixture projectFactory)
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
    [InlineData("C#", null)]
    [InlineData("C#", new[] { ArgConstants.UseProgramMain })]
    [InlineData("F#", null)]
    public async Task WorkerTemplateAsync(string language, string[] args)
    {
        await WorkerTemplateCoreAsync(language, args);

    }

    [ConditionalTheory(Skip = "Unskip when Helix supports native AOT. https://github.com/dotnet/aspnetcore/pull/47247/")]
    [InlineData("C#")]
    // [InlineData("F#")] F# doesn't fully support NativeAOT - https://github.com/dotnet/fsharp/issues/13398
    public async Task WorkerTemplateNativeAotAsync(string language)
    {
        await WorkerTemplateCoreAsync(language, args: new[] { ArgConstants.PublishNativeAot });
    }

    private async Task WorkerTemplateCoreAsync(string language, string[] args)
    {
        var nativeAot = args?.Contains(ArgConstants.PublishNativeAot) ?? false;

        var project = await ProjectFactory.CreateProject(Output);
        if (nativeAot)
        {
            project.SetCurrentRuntimeIdentifier();
        }

        await project.RunDotNetNewAsync("worker", language: language, args: args);

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
