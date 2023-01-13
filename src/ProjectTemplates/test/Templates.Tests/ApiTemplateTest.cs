// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit.Abstractions;

namespace Templates.Test;

public class ApiTemplateTest : LoggedTest
{
    public ApiTemplateTest(ProjectFactoryFixture factoryFixture)
    {
        ProjectFactory = factoryFixture;
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
    public async Task ApiTemplateCSharp()
    {
        await ApiTemplateCore(languageOverride: null);
    }

    [ConditionalFact(Skip = "Unskip when there are no more build or publish warnings for native AOT.")]
    public async Task ApiTemplateNativeAotCSharp()
    {
        await ApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.PublishNativeAot });
    }

    [ConditionalFact]
    public async Task ApiTemplateProgramMainCSharp()
    {
        await ApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain });
    }

    [ConditionalFact(Skip = "Unskip when there are no more build or publish warnings for native AOT.")]
    public async Task ApiTemplateProgramMainNativeAotCSharp()
    {
        await ApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain, ArgConstants.PublishNativeAot });
    }

    private async Task ApiTemplateCore(string languageOverride, string[] args = null)
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("api", args: args, language: languageOverride);

        var nativeAot = args?.Contains(ArgConstants.PublishNativeAot) ?? false;
        var expectedLaunchProfileNames = nativeAot
            ? new[] { "http" }
            : new[] { "http", "IIS Express" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
        if (languageOverride != null)
        {
            return;
        }

        // Force a restore if native AOT so that RID-specific assets are restored
        await project.RunDotNetPublishAsync(noRestore: !nativeAot);

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
               aspNetProcess.Process.HasExited,
               ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await AssertEndpoints(aspNetProcess);
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await AssertEndpoints(aspNetProcess);
        }
    }

    private async Task AssertEndpoints(AspNetProcess aspNetProcess)
    {
        await aspNetProcess.AssertOk("/todos");
        await aspNetProcess.AssertOk("/todos/1");
        await aspNetProcess.AssertNotFound("/todos/100");
        await aspNetProcess.AssertNotFound("/");
    }
}
