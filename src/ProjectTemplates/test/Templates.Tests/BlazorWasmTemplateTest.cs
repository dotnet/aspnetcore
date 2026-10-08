// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Extensions.Logging.Testing;
using Templates.Test.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test;

#pragma warning disable xUnit1041 // Fixture arguments to test classes must have fixture sources

public class BlazorWasmTemplateTest : LoggedTest
{
    public BlazorWasmTemplateTest(ProjectFactoryFixture projectFactory)
    {
        ProjectFactory = projectFactory;
    }

    public ProjectFactoryFixture ProjectFactory { get; }

    private ITestOutputHelper _output;
    public ITestOutputHelper Output
    {
        get
        {
            if (_output is null)
            {
                _output = new TestOutputLogger(Logger);
            }
            return _output;
        }
    }

    [Fact]
    public async Task BlazorWasm_Help_ShowsCorrectB2CInstanceDefault()
    {
        await TemplatePackageInstaller.EnsureTemplatingEngineInitializedAsync(Output);
        using var result = await TemplatePackageInstaller.RunDotNetNew(Output, "blazorwasm --help");

        Assert.True(result.ExitCode == 0, result.GetFormattedOutput());
        Assert.Matches(
            @"--aad-b2c-instance <aad-b2c-instance>\s+[^-]*?Default:\s+https://aadB2CInstance\.b2clogin\.com/",
            result.Output);
        Assert.DoesNotContain("https:////aadB2CInstance.b2clogin.com/", result.Output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlazorWasm_IndividualB2C_DefaultInstance_PreservesTenantPlaceholder()
    {
        var project = await ProjectFactory.CreateProject(Output);
        await project.RunDotNetNewRawAsync("new blazorwasm --auth IndividualB2C --no-restore");
        var appSettings = project.ReadFile("wwwroot/appsettings.json");

        Assert.Contains(
            "\"Authority\": \"https://aadB2CInstance.b2clogin.com/qualified.domain.name/b2c_1_susi\"",
            appSettings,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task BlazorWasm_IndividualB2C_CustomInstance_ReplacesAuthority()
    {
        var project = await ProjectFactory.CreateProject(Output);
        await project.RunDotNetNewRawAsync(
            "new blazorwasm --auth IndividualB2C --aad-b2c-instance https://example.b2clogin.com/ --domain my-domain -ssp b2c_1_siupin --no-restore");
        var appSettings = project.ReadFile("wwwroot/appsettings.json");

        Assert.Contains(
            "\"Authority\": \"https://example.b2clogin.com/my-domain/b2c_1_siupin\"",
            appSettings,
            StringComparison.Ordinal);
    }
}
