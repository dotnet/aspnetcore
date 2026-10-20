// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.InternalTesting;
using Templates.Test.Helpers;
using Xunit;

namespace Templates.Test;

public class BlazorWasmTemplateTest : LoggedTest
{
    [Fact]
    public async Task BlazorWasmTemplate_HelpDisplaysValidB2CInstanceDefault()
    {
        var output = new TestOutputLogger(Logger);
        await TemplatePackageInstaller.EnsureTemplatingEngineInitializedAsync(output);

        using var result = await TemplatePackageInstaller.RunDotNetNew(output, "blazorwasm --help");

        Assert.True(result.ExitCode == 0, result.GetFormattedOutput());
        Assert.Matches(@"Default:\s+https://aadB2CInstance\.b2clogin\.com/", result.Output);
        Assert.DoesNotContain("https:////aadB2CInstance.b2clogin.com/", result.Output);
    }
}
