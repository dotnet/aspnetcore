// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Mvc.Test;

public class RazorPagesTemplateTest : LoggedTest
{
    public RazorPagesTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

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
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public async Task RazorPagesTemplate_NoAuth(bool useProgramMain, bool noHttps)
    {
        var project = await ProjectFactory.CreateProject(Output);

        var args = useProgramMain
            ? noHttps
                ? new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps }
                : new[] { ArgConstants.UseProgramMain }
            : noHttps
                ? new[] { ArgConstants.NoHttps }
                : null;
        await project.RunDotNetNewAsync("razor", args: args);

        var expectedLaunchProfileNames = noHttps
            ? new[] { "http" }
            : new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        var projectFileContents = ReadFile(project.TemplateOutputDir, $"{project.ProjectName}.csproj");
        Assert.DoesNotContain("app.db", projectFileContents);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
        Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
        Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();

        var pages = new List<Page>
            {
                new Page
                {
                    Url = PageUrls.HomeUrl,
                    Links = new [] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.DocsUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.PrivacyUrl,
                    Links = new [] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.PrivacyUrl }
                }
            };

        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertPagesOk(pages);
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run published project", project, aspNetProcess.Process));

            await aspNetProcess.AssertPagesOk(pages);
        }
    }

    [ConditionalTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64 + HelixConstants.DebianAmd64)]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "No LocalDb on non-Windows")]
    public Task RazorPagesTemplate_IndividualAuth_LocalDb(bool useProgramMain, bool noHttps) => RazorPagesTemplate_IndividualAuth_Core(useLocalDB: true, useProgramMain, noHttps);

    [ConditionalTheory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64 + HelixConstants.DebianAmd64)]
    public Task RazorPagesTemplate_IndividualAuth(bool useProgramMain, bool noHttps) => RazorPagesTemplate_IndividualAuth_Core(useLocalDB: false, useProgramMain, noHttps);

    private async Task RazorPagesTemplate_IndividualAuth_Core(bool useLocalDB, bool useProgramMain, bool noHttps)
    {
        var project = await ProjectFactory.CreateProject(Output);

        var args = useProgramMain
            ? noHttps
                ? new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps }
                : new[] { ArgConstants.UseProgramMain }
            : noHttps
                ? new[] { ArgConstants.NoHttps }
                : null;
        await project.RunDotNetNewAsync("razor", auth: "Individual", useLocalDB: useLocalDB, args: args);

        // Individual auth supports no https OK
        var expectedLaunchProfileNames = noHttps
            ? new[] { "http" }
            : new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        var projectFileContents = ReadFile(project.TemplateOutputDir, $"{project.ProjectName}.csproj");
        if (!useLocalDB)
        {
            Assert.Contains("app.db", projectFileContents);
        }

        await project.RunDotNetPublishAsync();

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.

        await project.RunDotNetBuildAsync();

        await project.RunDotNetEfCreateMigrationAsync("razorpages");
        project.AssertEmptyMigration("razorpages");

        // Note: if any links are updated here, MvcTemplateTest.cs should be updated as well
        var pages = new List<Page> {
                new Page
                {
                    Url = PageUrls.ForgotPassword,
                    Links = new [] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.HomeUrl,
                    Links = new [] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.DocsUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.PrivacyUrl,
                    Links = new [] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.PrivacyUrl
                    }
                },
                new Page
                {
                    Url = PageUrls.LoginUrl,
                    Links = new [] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.ForgotPassword,
                        PageUrls.RegisterUrl,
                        PageUrls.ResendEmailConfirmation,
                        PageUrls.ExternalArticle,
                        PageUrls.PrivacyUrl }
                },
                new Page
                {
                    Url = PageUrls.RegisterUrl,
                    Links = new [] {
                        PageUrls.HomeUrl,
                        PageUrls.HomeUrl,
                        PageUrls.PrivacyUrl,
                        PageUrls.RegisterUrl,
                        PageUrls.LoginUrl,
                        PageUrls.ExternalArticle,
                        PageUrls.PrivacyUrl
                    }
                }
            };

        using (var aspNetProcess = project.StartBuiltProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertPagesOk(pages);
        }

        using (var aspNetProcess = project.StartPublishedProjectAsync())
        {
            Assert.False(
                aspNetProcess.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, aspNetProcess.Process));

            await aspNetProcess.AssertPagesOk(pages);
        }
    }

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("IndividualB2C", null)]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain })]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("IndividualB2C", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("IndividualB2C", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    public Task RazorPagesTemplate_IdentityWeb_IndividualB2C_BuildsAndPublishes(string auth, string[] args) => BuildAndPublishRazorPagesTemplateIdentityWeb(auth: auth, args: args);

    [ConditionalTheory]
    [SkipOnHelix("https://github.com/dotnet/aspnetcore/issues/28090", Queues = HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [InlineData("SingleOrg", null)]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new[] { ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CalledApiUrlGraphMicrosoftCom, ArgConstants.CalledApiScopesUserReadWrite, ArgConstants.NoHttps })]
    public Task RazorPagesTemplate_IdentityWeb_SingleOrg_BuildsAndPublishes(string auth, string[] args) => BuildAndPublishRazorPagesTemplateIdentityWeb(auth: auth, args: args);

    [ConditionalTheory]
    [InlineData("SingleOrg", new[] { ArgConstants.CallsGraph })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph })]
    [InlineData("SingleOrg", new[] { ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    [InlineData("SingleOrg", new[] { ArgConstants.UseProgramMain, ArgConstants.CallsGraph, ArgConstants.NoHttps })]
    public Task RazorPagesTemplate_IdentityWeb_SingleOrg_CallsGraph_BuildsAndPublishes(string auth, string[] args) => BuildAndPublishRazorPagesTemplateIdentityWeb(auth: auth, args: args);

    private async Task<Project> BuildAndPublishRazorPagesTemplateIdentityWeb(string auth, string[] args)
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("razor", auth: auth, args: args);

        // Identity Web auth requires https and thus ignores the --no-https option if passed so there should never be an 'http' profile
        var expectedLaunchProfileNames = new[] { "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        // Verify building in debug works
        await project.RunDotNetBuildAsync();

        // Publish builds in "release" configuration. Running publish should ensure we can compile in release and that we can publish without issues.
        await project.RunDotNetPublishAsync();

        return project;
    }

    private string ReadFile(string basePath, string path)
    {
        var fullPath = Path.Combine(basePath, path);
        var doesExist = File.Exists(fullPath);

        Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
        return File.ReadAllText(Path.Combine(basePath, path));
    }
}
