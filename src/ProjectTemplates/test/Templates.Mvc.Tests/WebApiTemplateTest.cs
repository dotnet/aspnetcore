// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Newtonsoft.Json;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Mvc.Test;

public class WebApiTemplateTest : LoggedTest
{
    public WebApiTemplateTest(ProjectFactoryFixture factoryFixture)
    {
        FactoryFixture = factoryFixture;
    }

    public ProjectFactoryFixture FactoryFixture { get; }

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
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("IndividualB2C", null)]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseControllers })]
    [InlineData("IndividualB2C", new [] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    public Task WebApiTemplateCSharp_IdentityWeb_IndividualB2C_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("IndividualB2C", new[] { ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseControllers, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new [] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    public Task WebApiTemplateCSharp_IdentityWeb_IndividualB2C_NoHttps_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    public Task WebApiTemplateCSharp_IdentityWeb_IndividualB2C_ProgramMain_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    public Task WebApiTemplateCSharp_IdentityWeb_IndividualB2C_ProgramMain_NoHttps_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("SingleOrg", null)]
    [InlineData("SingleOrg", new [] { ArgConstants.UseControllers })]
    [InlineData("SingleOrg", new [] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new [] { ArgConstants.CallsGraph })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseControllers, ArgConstants.CallsGraph })]
    public Task WebApiTemplateCSharp_IdentityWeb_SingleOrg_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("SingleOrg", new [] { ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseControllers, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseControllers, ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    public Task WebApiTemplateCSharp_IdentityWeb_SingleOrg_NoHttps_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.CallsGraph })]
    public Task WebApiTemplateCSharp_IdentityWeb_SingleOrg_ProgramMain_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("SingleOrg", null)]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new [] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    public Task WebApiTemplateCSharp_IdentityWeb_SingleOrg_ProgramMain_NoHttps_BuildsAndPublishes(string auth, string[] args) => PublishAndBuildWebApiTemplate(languageOverride: null, auth: auth, args: args);

    [Fact]
    public Task WebApiTemplateFSharp() => WebApiTemplateCore(languageOverride: "F#");

    [Fact]
    public Task WebApiTemplateNoHttpsFSharp() => WebApiTemplateCore(languageOverride: "F#", args: new[] { ArgConstants.NoHttps });

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateCSharp() => WebApiTemplateCore(languageOverride: null);

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateNoHttpsCSharp() => WebApiTemplateCore(languageOverride: null, new[] { ArgConstants.NoHttps });

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateProgramMainCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain });

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateProgramMainNoHttpsCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps });

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateControllersCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseControllers });

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateControllersNoHttpsCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseControllers, ArgConstants.NoHttps });

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateProgramMainControllersCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain, ArgConstants.UseControllers });

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task WebApiTemplateProgramMainControllersNoHttpsCSharp() => WebApiTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.NoHttps });

    [ConditionalTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task WebApiTemplateCSharp_WithoutOpenAPI(bool useProgramMain, bool useControllers)
    {
        var project = await FactoryFixture.CreateProject(Output);

        var args = useProgramMain
            ? useControllers
                ? new[] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.NoOpenApi }
                : new[] { ArgConstants.UseProgramMain, ArgConstants.NoOpenApi }
            : useControllers
                ? new[] { ArgConstants.UseControllers, ArgConstants.NoOpenApi }
                : new[] { ArgConstants.NoOpenApi };
        await project.RunDotNetNewAsync("webapi", args: args);

        await project.RunDotNetBuildAsync();

        using var aspNetProcess = project.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

        await aspNetProcess.AssertNotFound("openapi/v1.json");
    }

    [ConditionalTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task WebApiTemplateCSharpNoHttps_WithoutOpenAPI(bool useProgramMain, bool useControllers)
    {
        var project = await FactoryFixture.CreateProject(Output);

        var args = useProgramMain
            ? useControllers
                ? new[] { ArgConstants.UseProgramMain, ArgConstants.UseControllers, ArgConstants.NoOpenApi, ArgConstants.NoHttps }
                : new[] { ArgConstants.UseProgramMain, ArgConstants.NoOpenApi, ArgConstants.NoHttps }
            : useControllers
                ? new[] { ArgConstants.UseControllers, ArgConstants.NoOpenApi, ArgConstants.NoHttps }
                : new[] { ArgConstants.NoOpenApi, ArgConstants.NoHttps };
        await project.RunDotNetNewAsync("webapi", args: args);

        var noHttps = args.Contains(ArgConstants.NoHttps);
        var expectedLaunchProfileNames = noHttps
            ? new[] { "http" }
            : new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        await project.RunDotNetBuildAsync();

        using var aspNetProcess = project.StartBuiltProjectAsync();
        Assert.False(
            aspNetProcess.Process.HasExited,
            ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

        await aspNetProcess.AssertNotFound("openapi/v1.json");
    }

    private async Task<Project> PublishAndBuildWebApiTemplate(string languageOverride, string auth, string[] args = null)
    {
        var project = await FactoryFixture.CreateProject(Output);

        await project.RunDotNetNewAsync("webapi", language: languageOverride, auth: auth, args: args);

        // External auth mechanisms require https to work and thus don't honor the --no-https flag
        var requiresHttps = string.Equals(auth, "IndividualB2C", StringComparison.OrdinalIgnoreCase)
                            || string.Equals(auth, "SingleOrg", StringComparison.OrdinalIgnoreCase);
        var noHttps = args?.Contains(ArgConstants.NoHttps) ?? false;
        var expectedLaunchProfileNames = requiresHttps
            ? new[] { "https" }
            : noHttps
                ? new[] { "http" }
                : new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
        if (languageOverride != null)
        {
            return project;
        }

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();

        return project;
    }

    private async Task WebApiTemplateCore(string languageOverride, string[] args = null)
    {
        var project = await PublishAndBuildWebApiTemplate(languageOverride, null, args);

        // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
        if (languageOverride != null)
        {
            return;
        }

        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertOk("weatherforecast");
            await aspNetProcess.AssertOk("openapi/v1.json");
            await aspNetProcess.AssertNotFound("/");
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertOk("weatherforecast");
            // OpenAPI endpoint is only enabled in Development
            await aspNetProcess.AssertNotFound("openapi/v1.json");
            await aspNetProcess.AssertNotFound("/");
        }
    }
}
