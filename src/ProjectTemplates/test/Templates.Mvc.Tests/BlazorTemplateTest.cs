// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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

    [ConditionalTheory]
    [InlineData("ResetPassword", "InvalidLength")]
    [InlineData("ResetPassword", "TrailingPeriod")]
    [InlineData("ConfirmEmail", "InvalidLength")]
    [InlineData("ConfirmEmail", "TrailingPeriod")]
    [InlineData("ConfirmEmailChange", "InvalidLength")]
    [InlineData("ConfirmEmailChange", "TrailingPeriod")]
    [SkipOnHelix("Cert failure, https://github.com/dotnet/aspnetcore/issues/28090", Queues = "All.OSX;" + HelixConstants.Windows10Arm64 + HelixConstants.DebianArm64)]
    public async Task BlazorWebTemplate_MalformedAccountCode_ShowsErrorAndPreservesValidLink(string page, string malformedCase)
    {
        var project = await ProjectFactory.CreateProject(TestOutputHelper);
        await project.RunDotNetNewAsync("blazor", args: [ArgConstants.IndividualAuth, ArgConstants.NoInteractivity, ArgConstants.NoHttps]);

        var programPath = Path.Combine(project.TemplateOutputDir, "Program.cs");
        var program = await File.ReadAllTextAsync(programPath);
        const string runMarker = "app.Run();";
        Assert.Contains(runMarker, program);
        program = program.Replace(runMarker, """
            app.MapPost("/test/account-code", async (UserManager<ApplicationUser> users, ApplicationDbContext db) =>
            {
                await db.Database.EnsureCreatedAsync();
                var user = new ApplicationUser { UserName = "original@example.com", Email = "original@example.com" };
                var result = await users.CreateAsync(user, "Password1!");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(", ", result.Errors.Select(error => error.Description)));
                }

                static string Encode(string token) => Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(token));

                return new
                {
                    UserId = user.Id,
                    user.Email,
                    ResetCode = Encode(await users.GeneratePasswordResetTokenAsync(user)),
                    ConfirmCode = Encode(await users.GenerateEmailConfirmationTokenAsync(user)),
                    ChangeCode = Encode(await users.GenerateChangeEmailTokenAsync(user, "changed@example.com")),
                };
            });
            app.MapGet("/test/account-code/{userId}", async (string userId, UserManager<ApplicationUser> users) =>
            {
                var user = await users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Test user was not found.");
                return new { user.Email, user.UserName, user.EmailConfirmed };
            });
            app.Run();
            """, StringComparison.Ordinal);
        await File.WriteAllTextAsync(programPath, program);
        await project.RunDotNetBuildAsync();

        using var app = project.StartBuiltProjectAsync(noHttps: true);
        using var client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = app.ListeningUri };
        using var setupResponse = await client.PostAsync("/test/account-code", content: null);
        setupResponse.EnsureSuccessStatusCode();
        var account = await setupResponse.Content.ReadFromJsonAsync<AccountCodes>();
        Assert.NotNull(account);

        using var missingResponse = await client.GetAsync($"/Account/{page}");
        Assert.Equal(HttpStatusCode.Redirect, missingResponse.StatusCode);

        if (page is not "ResetPassword")
        {
            var unknownUrl = page is "ConfirmEmail"
                ? "/Account/ConfirmEmail?userId=unknown-user&code=a"
                : "/Account/ConfirmEmailChange?userId=unknown-user&email=changed%40example.com&code=a";
            using var unknownResponse = await client.GetAsync(unknownUrl);
            Assert.Equal(page is "ConfirmEmail" ? HttpStatusCode.NotFound : HttpStatusCode.OK, unknownResponse.StatusCode);
        }

        var validCode = page switch
        {
            "ResetPassword" => account.ResetCode,
            "ConfirmEmail" => account.ConfirmCode,
            "ConfirmEmailChange" => account.ChangeCode,
            _ => throw new ArgumentOutOfRangeException(nameof(page)),
        };
        var malformedCode = malformedCase switch
        {
            "InvalidLength" => "a",
            "TrailingPeriod" => validCode + ".",
            _ => throw new ArgumentOutOfRangeException(nameof(malformedCase)),
        };

        string Link(string code) => page switch
        {
            "ResetPassword" => $"/Account/ResetPassword?code={Uri.EscapeDataString(code)}",
            "ConfirmEmail" => $"/Account/ConfirmEmail?userId={Uri.EscapeDataString(account.UserId)}&code={Uri.EscapeDataString(code)}",
            "ConfirmEmailChange" => $"/Account/ConfirmEmailChange?userId={Uri.EscapeDataString(account.UserId)}&email=changed%40example.com&code={Uri.EscapeDataString(code)}",
            _ => throw new ArgumentOutOfRangeException(nameof(page)),
        };

        using var malformedResponse = await client.GetAsync(Link(malformedCode));
        if (page is "ResetPassword")
        {
            Assert.Equal(HttpStatusCode.Redirect, malformedResponse.StatusCode);
            Assert.NotNull(malformedResponse.Headers.Location);
            Assert.Equal("/Account/InvalidPasswordReset", new Uri(client.BaseAddress!, malformedResponse.Headers.Location).AbsolutePath);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, malformedResponse.StatusCode);
            var body = await malformedResponse.Content.ReadAsStringAsync();
            Assert.Contains(page is "ConfirmEmail" ? "Error confirming your email." : "Error changing email.", body);
        }

        var unchanged = await client.GetFromJsonAsync<AccountState>($"/test/account-code/{account.UserId}");
        Assert.NotNull(unchanged);
        Assert.Equal(account.Email, unchanged.Email);
        Assert.Equal(account.Email, unchanged.UserName);
        Assert.False(unchanged.EmailConfirmed);

        using var validResponse = await client.GetAsync(Link(validCode));
        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        var validBody = await validResponse.Content.ReadAsStringAsync();
        Assert.Contains(page switch
        {
            "ResetPassword" => "name=\"Input.Code\"",
            "ConfirmEmail" => "Thank you for confirming your email.",
            "ConfirmEmailChange" => "Thank you for confirming your email change.",
            _ => throw new ArgumentOutOfRangeException(nameof(page)),
        }, validBody);

        if (page is "ConfirmEmailChange")
        {
            var changed = await client.GetFromJsonAsync<AccountState>($"/test/account-code/{account.UserId}");
            Assert.NotNull(changed);
            Assert.Equal("changed@example.com", changed.Email);
            Assert.Equal("changed@example.com", changed.UserName);
        }
        else if (page is "ConfirmEmail")
        {
            var confirmed = await client.GetFromJsonAsync<AccountState>($"/test/account-code/{account.UserId}");
            Assert.NotNull(confirmed);
            Assert.True(confirmed.EmailConfirmed);
        }
    }

    private sealed record AccountCodes(string UserId, string Email, string ResetCode, string ConfirmCode, string ChangeCode);
    private sealed record AccountState(string Email, string UserName, bool EmailConfirmed);

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

        if (args.Contains(ArgConstants.IndividualAuth) && !args.Contains(ArgConstants.UseLocalDb))
        {
            await project.RunDotNetEfUpdateDatabaseAsync();
            await project.RunDotNetEfCreateMigrationAsync("blazor");
            project.AssertEmptyMigration("blazor");
        }

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
