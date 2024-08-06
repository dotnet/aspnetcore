// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit.Sdk;

namespace Templates.Mvc.Test;

public class BlazorTemplateTest : LoggedTest
{
    public BlazorTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; set; }

    public static TheoryData<string[]> ArgsData() =>
    [
        [],
        [ArgConstants.UseProgramMain],
        [ArgConstants.NoHttps],
        [ArgConstants.Empty],
        [ArgConstants.NoInteractivity],
        [ArgConstants.WebAssemblyInteractivity],
        [ArgConstants.AutoInteractivity],
        [ArgConstants.GlobalInteractivity],
        [ArgConstants.GlobalInteractivity, ArgConstants.WebAssemblyInteractivity],
        [ArgConstants.GlobalInteractivity, ArgConstants.AutoInteractivity],
        [ArgConstants.NoInteractivity, ArgConstants.UseProgramMain, ArgConstants.NoHttps, ArgConstants.Empty],
    ];

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
            ? new[] { "http" }
            : new[] { "http", "https" };
        await project.VerifyLaunchSettings(expectedLaunchProfileNames);

        var projectFileContents = await ReadProjectFileAsync(project);
        Assert.DoesNotContain("Microsoft.VisualStudio.Web.CodeGeneration.Design", projectFileContents);
        Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools.DotNet", projectFileContents);
        Assert.DoesNotContain("Microsoft.Extensions.SecretManager.Tools", projectFileContents);

        if (!args.Contains(ArgConstants.IndividualAuth))
        {
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore.Tools", projectFileContents);
            Assert.DoesNotContain("app.db", projectFileContents);
        }
        else
        {
            Assert.Contains("Microsoft.EntityFrameworkCore.Tools", projectFileContents);

            if (args.Contains(ArgConstants.UseLocalDb))
            {
                Assert.DoesNotContain("app.db", projectFileContents);
            }
            else
            {
                Assert.Contains("app.db", projectFileContents);
            }
        }

        // This can be removed once https://github.com/dotnet/razor/issues/9343 is fixed.
        await WorkAroundNonNullableRenderModeAsync(project);

        // Run dotnet build after publish. The reason is that one uses Config = Debug and the other uses Config = Release
        // The output from publish will go into bin/Release/netcoreappX.Y/publish and won't be affected by calling build
        // later, while the opposite is not true.
        await project.RunDotNetPublishAsync();
        await project.RunDotNetBuildAsync();
        var expectedPages = GetExpectedPages(args);
        var unexpectedPages = GetUnxpectedPages(args);

        async Task VerifyProcessAsync(AspNetProcess process)
        {
            Assert.False(
                process.Process.HasExited,
                ErrorMessages.GetFailedProcessMessageOrEmpty("Run built project", project, process.Process));

            await process.AssertPagesOk(expectedPages);
            await process.AssertPagesNotFound(unexpectedPages);

            if (args.Contains(ArgConstants.IndividualAuth) && !args.Contains(ArgConstants.Empty))
            {
                var response = await process.SendRequest(BlazorTemplatePages.Auth);
                response.EnsureSuccessStatusCode();
                Assert.Equal("/Account/Login?ReturnUrl=%2Fauth", response.RequestMessage.RequestUri.PathAndQuery);
            }
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
            yield return new(BlazorTemplatePages.Login);
            yield return new(BlazorTemplatePages.Register);
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

    private static IEnumerable<string> GetUnxpectedPages(string[] args)
    {
        if (!args.Contains(ArgConstants.IndividualAuth))
        {
            yield return BlazorTemplatePages.Auth;
            yield return BlazorTemplatePages.Login;
        }

        if (args.Contains(ArgConstants.Empty))
        {
            yield return BlazorTemplatePages.Weather;
            yield return BlazorTemplatePages.Counter;
            yield return BlazorTemplatePages.Auth;
        }

        if (args.Contains(ArgConstants.NoInteractivity))
        {
            yield return BlazorTemplatePages.Counter;
        }
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

        throw FailException.ForFailure($"Expected file to exist, but it doesn't: {singleProjectPath}");
    }

    private async Task WorkAroundNonNullableRenderModeAsync(Project project)
    {
        var appRazorPath = Path.Combine(project.TemplateOutputDir, "Components", "App.razor");
        var appRazorText = await File.ReadAllTextAsync(appRazorPath);
        appRazorText = appRazorText.Replace("IComponentRenderMode?", "IComponentRenderMode").Replace(": null", ": null!");
        await File.WriteAllTextAsync(appRazorPath, appRazorText);
    }

    private class BlazorTemplatePages
    {
        internal const string Index = "";
        internal const string Weather = "weather";
        internal const string Counter = "counter";
        internal const string Auth = "auth";
        internal const string Login = "Account/Login";
        internal const string Register = "Account/Register";
        internal const string ForgotPassword = "Account/ForgotPassword";
        internal const string ResendEmailConfirmation = "Account/ResendEmailConfirmation";
    }
}
