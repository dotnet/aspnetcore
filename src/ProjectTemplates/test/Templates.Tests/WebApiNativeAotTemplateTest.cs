// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using Templates.Test.Helpers;
using Xunit.Abstractions;

namespace Templates.Test;

public class WebApiNativeAotTemplateTest : LoggedTest
{
    public WebApiNativeAotTemplateTest(ProjectFactoryFixture factoryFixture)
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
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/47478", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task WebApiNativeAotTemplateCSharp()
    {
        await WebApiNativeAotTemplateCore(languageOverride: null);
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/47478", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task WebApiNativeAotTemplateProgramMainCSharp()
    {
        await WebApiNativeAotTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain });
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/47478", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task WebApiTemplateCSharp_WithoutOpenAPI(bool useProgramMain)
    {
        var project = await FactoryFixture.CreateProject(Output);

        var args = useProgramMain
            ? new[] { ArgConstants.UseProgramMain, ArgConstants.NoOpenApi }
            : new[] { ArgConstants.NoOpenApi };

        await project.RunDotNetNewAsync("webapi", args: args);

        await project.RunDotNetBuildAsync();

        using var aspNetProcess = project.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

        await aspNetProcess.AssertNotFound("openapi/v1.json");
    }

    private async Task WebApiNativeAotTemplateCore(string languageOverride, string[] args = null)
    {
        var project = await ProjectFactory.CreateProject(Output);
        project.SetCurrentRuntimeIdentifier();

        await project.RunDotNetNewAsync("webapiaot", args: args, language: languageOverride);

        var expectedLaunchProfileNames = new[] { "http" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        await project.VerifyHasProperty("InvariantGlobalization", "true");

        // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
        if (languageOverride != null)
        {
            return;
        }

        // Force a restore for native AOT so that RID-specific assets are restored
        await project.RunDotNetPublishAsync(noRestore: false);

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();

        // The minimal/slim/core scenario doesn't include TLS support, so tell `project` not to register an https address
        using (var aspNetProcess = project.StartBuiltProjectAsync(noHttps: true))
        {
            Assert.False(
               aspNetProcess.Process.HasExited,
               ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));
            await AssertEndpoints(aspNetProcess);
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync(noHttps: true, usePublishedAppHost: true))
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
