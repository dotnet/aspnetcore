// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

public class EmptyWebTemplateTest : LoggedTest
{
    public EmptyWebTemplateTest(ProjectFactoryFixture projectFactory)
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
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task EmptyWebTemplateCSharp()
    {
        await EmtpyTemplateCore(languageOverride: null);
    }

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task EmptyWebTemplateNoHttpsCSharp()
    {
        await EmtpyTemplateCore(languageOverride: null, args: new[] { ArgConstants.NoHttps });
    }

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task EmptyWebTemplateProgramMainCSharp()
    {
        await EmtpyTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain });
    }

    [ConditionalFact]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task EmptyWebTemplateProgramMainNoHttpsCSharp()
    {
        await EmtpyTemplateCore(languageOverride: null, args: new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps });
    }

    [Fact]
    public async Task EmptyWebTemplateFSharp()
    {
        await EmtpyTemplateCore("F#");
    }

    [Fact]
    public async Task EmptyWebTemplateNoHttpsFSharp()
    {
        await EmtpyTemplateCore("F#", args: new[] { ArgConstants.NoHttps });
    }

    private async Task EmtpyTemplateCore(string languageOverride, string[] args = null)
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("web", args: args, language: languageOverride);

        var noHttps = args?.Contains(ArgConstants.NoHttps) ?? false;
        var expectedLaunchProfileNames = noHttps
            ? new[] { "http" }
            : new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        // Avoid the F# compiler. See https://github.com/dotnet/aspnetcore/issues/14022
        if (languageOverride != null)
        {
            return;
        }

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();

        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
               aspNetProcess.Process.HasExited,
               ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertOk("/");
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertOk("/");
        }
    }
}
