// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit.Abstractions;

namespace Templates.Blazor.Tests;
public class BlazorWebTemplateTest : LoggedTest
{
    public BlazorWebTemplateTest(ProjectFactoryFixture projectFactory)
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
    [MemberData(nameof(ArgsData))]
    public async Task BlazorWebTemplate_NoAuth(string[] args)
    {
        var project = await ProjectFactory.CreateProject(Output);

        await project.RunDotNetNewAsync("blazor", args: args);

        var expectedLaunchProfileNames = args.Contains(ArgConstants.NoHttps)
            ? new[] { "http", "IIS Express" }
            : new[] { "http", "https", "IIS Express" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        var projectFileContents = ReadFile(project.TemplateOutputDir, $"{project.ProjectName}.csproj");
        Assert.DoesNotContain(".db", projectFileContents);
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
                Url = BlazorTemplatePages.Index,
            },
            new Page
            {
                Url = BlazorTemplatePages.Weather,
            }
        };

        if (!args.Contains(ArgConstants.NoInteractivity))
        {
            pages.Add(new Page
            {
                Url = BlazorTemplatePages.Counter,
            });
        }

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

    public static TheoryData<string[]> ArgsData() => new TheoryData<string[]>
    {
        new string[0],
        new[] { ArgConstants.UseProgramMain },
        new[] { ArgConstants.NoHttps },
        new[] { ArgConstants.UseProgramMain, ArgConstants.NoHttps },
        new[] { ArgConstants.NoInteractivity },
        new[] { ArgConstants.NoInteractivity, ArgConstants.UseProgramMain },
        new[] { ArgConstants.NoInteractivity, ArgConstants.NoHttps },
        new[] { ArgConstants.NoInteractivity, ArgConstants.UseProgramMain, ArgConstants.NoHttps }
    };

    private string ReadFile(string basePath, string path)
    {
        var fullPath = Path.Combine(basePath, path);
        var doesExist = File.Exists(fullPath);

        Assert.True(doesExist, $"Expected file to exist, but it doesn't: {path}");
        return File.ReadAllText(Path.Combine(basePath, path));
    }

    private class BlazorTemplatePages
    {
        internal static readonly string Index = "";
        internal static readonly string Weather = "weather";
        internal static readonly string Counter = "counter";
    }   
}
