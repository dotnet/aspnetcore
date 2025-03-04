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
        await WebApiNativeAotTemplateCore(
            languageOverride: null,
            additionalEndpointsThatShould200OkForBuiltProjects: new[] { "/openapi/v1.json" });
    }

    [ConditionalFact]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/47478", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task WebApiNativeAotTemplateProgramMainCSharp()
    {
        await WebApiNativeAotTemplateCore(
            languageOverride: null,
            args: new[] { ArgConstants.UseProgramMain },
            additionalEndpointsThatShould200OkForBuiltProjects: new[] { "/openapi/v1.json" });
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/47478", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task WebApiNativeAotTemplateCSharp_OpenApiDisabledInProductionEnvironment(bool useProgramMain)
    {
        var args = useProgramMain
            ? new[] { ArgConstants.UseProgramMain }
            : new string[] { };

        await WebApiNativeAotTemplateCore(
            languageOverride: null,
            args: args,
            additionalEndpointsThatShould404NotFoundForPublishedProjects: new[] { "/openapi/v1.json" });
    }

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/47478", Queues = HelixConstants.NativeAotNotSupportedHelixQueues)]
    public async Task WebApiNativeAotTemplateCSharp_WithoutOpenAPI(bool useProgramMain)
    {
        var args = useProgramMain
            ? new[] { ArgConstants.UseProgramMain, ArgConstants.NoOpenApi }
            : new[] { ArgConstants.NoOpenApi };

        await WebApiNativeAotTemplateCore(
            languageOverride: null,
            args: args,
            additionalEndpointsThatShould404NotFoundForBuiltProjects: new[] { "/openapi/v1.json" });
    }

    private async Task WebApiNativeAotTemplateCore(
        string languageOverride,
        string[] args = null,
        string[] additionalEndpointsThatShould200OkForBuiltProjects = null,
        string[] additionalEndpointsThatShould200OkForPublishedProjects = null,
        string[] additionalEndpointsThatShould404NotFoundForBuiltProjects = null,
        string[] additionalEndpointsThatShould404NotFoundForPublishedProjects = null)
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
            await AssertEndpoints(aspNetProcess, additionalEndpointsThatShould200OkForBuiltProjects, additionalEndpointsThatShould404NotFoundForBuiltProjects);
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync(noHttps: true, usePublishedAppHost: true))
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await AssertEndpoints(aspNetProcess, additionalEndpointsThatShould200OkForPublishedProjects, additionalEndpointsThatShould404NotFoundForPublishedProjects);
        }
    }

    private async Task AssertEndpoints(AspNetProcess aspNetProcess, string[] additionalEndpointsThatShould200Ok = null, string[] additionalEndpointsThatShould404NotFound = null)
    {
        await aspNetProcess.AssertOk("/todos");
        await aspNetProcess.AssertOk("/todos/1");
        await aspNetProcess.AssertNotFound("/todos/100");
        await aspNetProcess.AssertNotFound("/");

        if (additionalEndpointsThatShould200Ok is not null)
        {
            foreach (var endpoint in additionalEndpointsThatShould200Ok)
            {
                await aspNetProcess.AssertOk(endpoint);
            }
        }

        if (additionalEndpointsThatShould404NotFound is not null)
        {
            foreach (var endpoint in additionalEndpointsThatShould404NotFound)
            {
                await aspNetProcess.AssertNotFound(endpoint);
            }
        }
    }
}
