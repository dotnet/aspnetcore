// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Testing;
using Templates.Test.Helpers;
using Xunit.Sdk;

namespace Templates.Blazor.Tests;
public class BlazorWebTemplateTest : LoggedTest
{
    public BlazorWebTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

    public static TheoryData<string[]> ArgsData() => new TheoryData<string[]>
    {
        new string[0],
        new[] { ArgConstants.UseProgramMain },
        new[] { ArgConstants.NoHttps },
        new[] { ArgConstants.Empty },
        new[] { ArgConstants.NoInteractivity },
        new[] { ArgConstants.WebAssemblyInteractivity },
        new[] { ArgConstants.AutoInteractivity },
        new[] { ArgConstants.GlobalInteractivity },
        new[] { ArgConstants.GlobalInteractivity, ArgConstants.WebAssemblyInteractivity },
        new[] { ArgConstants.GlobalInteractivity, ArgConstants.AutoInteractivity },
        new[] { ArgConstants.NoInteractivity, ArgConstants.UseProgramMain, ArgConstants.NoHttps, ArgConstants.Empty },
    };

    [ConditionalTheory]
    [MemberData(nameof(ArgsData))]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task BlazorWebTemplate_NoAuth(string[] args) => BlazorWebTemplate_Core(args);

    [ConditionalTheory]
    [MemberData(nameof(ArgsData))]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public Task BlazorWebTemplate_IndividualAuth(string[] args) => BlazorWebTemplate_Core([ArgConstants.IndividualAuth, ..args]);

    [ConditionalTheory]
    [InlineData(false)]
    [InlineData(true)]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX, SkipReason = "No LocalDb on non-Windows")]
    public Task BlazorWebTemplate_IndividualAuth_LocalDb(bool useProgramMain) => useProgramMain
        ? BlazorWebTemplate_Core([ArgConstants.IndividualAuth, ArgConstants.UseLocalDb, ArgConstants.UseProgramMain])
        : BlazorWebTemplate_Core([ArgConstants.IndividualAuth, ArgConstants.UseLocalDb]);

    private async Task BlazorWebTemplate_Core(string[] args)
    {
        var project = await ProjectFactory.CreateProject(TestOutputHelper);

        await project.RunDotNetNewAsync("blazor", args: args);

        var expectedLaunchProfileNames = args.Contains(ArgConstants.NoHttps)
            ? new[] { "http", "IIS Express" }
            : new[] { "http", "https", "IIS Express" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        var projectFileContents = await ReadProjectFileAsync(project);
        Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
        Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

        if (!args.Contains(ArgConstants.IndividualAuth))
        {
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain(".db", projectFileContents);
        }
        else
        {
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools", projectFileContents);

            if (args.Contains(ArgConstants.UseLocalDb))
            {
                Assert.DoesNotContain(".db", projectFileContents);
            }
            else
            {
                Assert.Contains(".db", projectFileContents);
            }
        }

        // This can be removed once https://github.com/dotnet/razor/issues/9343 is fixed.
        await WorkAroundNonNullableRenderModeAsync(project);

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.
        await project.RunDotNetPublishAsync();
        await project.RunDotNetBuildAsync();
        var pages = GetExpectedPages(args);

        Task VerifyProcessAsync(AspNetProcess process)
        {
            Assert.False(
                process.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, process.Process));

            return process.AssertPagesOk(pages);
        }

        using (var process = project.StartBuiltProjectAsync())
        {
            await VerifyProcessAsync(process);
        }
        using (var process = project.StartPublishedProjectAsync())
        {
            await VerifyProcessAsync(process);
        }
    }

    private static IEnumerable<Page> GetExpectedPages(string[] args)
    {
        yield return new(BlazorTemplatePages.Index);

        if (args.Contains(ArgConstants.IndividualAuth))
        {
            yield return new(BlazorTemplatePages.LoginUrl);
            yield return new(BlazorTemplatePages.RegisterUrl);
            yield return new(BlazorTemplatePages.ForgotPassword);
            yield return new(BlazorTemplatePages.ResendEmailConfirmation);
        }

        if (args.Contains(ArgConstants.Empty))
        {
            yield break;
        }

        yield return new(BlazorTemplatePages.Weather);

        if (args.Contains(ArgConstants.NoInteractivity))
        {
            yield break;
        }

        yield return new(BlazorTemplatePages.Counter);
    }

    private Task<string> ReadProjectFileAsync(Project project)
    {
        var singleProjectPath = Path.Combine(project.TemplateOutputDir, $"{project.ProjectName}.csproj");
        if (File.Exists(singleProjectPath))
        {
            return File.ReadAllTextAsync(singleProjectPath);
        }

        var multiProjectPath = Path.Combine(project.TemplateOutputDir, project.ProjectName, $"{project.ProjectName}.csproj");
        if (File.Exists(multiProjectPath))
        {
            // Change the TemplateOutputDir to that of the main project.
            project.TemplateOutputDir = Path.GetDirectoryName(multiProjectPath);
            return File.ReadAllTextAsync(multiProjectPath);
        }

        throw new FailException($"Expected file to exist, but it doesn't: {singleProjectPath}");
    }

    private async Task WorkAroundNonNullableRenderModeAsync(Project project)
    {
        var appRazorPath = Path.Combine(project.TemplateOutputDir, "Components", "App.razor");
        var appRazorText = await File.ReadAllTextAsync(appRazorPath);
        appRazorText = appRazorText.Replace("IComponentRenderMode?", "IComponentRenderMode").Replace("? null", "? null!");
        await File.WriteAllTextAsync(appRazorPath, appRazorText);
    }

    private class BlazorTemplatePages
    {
        internal static readonly string Index = "";
        internal static readonly string Weather = "weather";
        internal static readonly string Counter = "counter";
        internal static readonly string LoginUrl = "Account/Login";
        internal static readonly string RegisterUrl = "Account/Register";
        internal static readonly string ForgotPassword = "Account/ForgotPassword";
        internal static readonly string ResendEmailConfirmation = "Account/ResendEmailConfirmation";
    }
}
