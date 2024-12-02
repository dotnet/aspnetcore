// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.InternalTesting;

namespace Microsoft.AspNetCore.Components.WebViewE2E.Test;

public class WebViewManagerE2ETests
{
    // Skips:
    // - Ubuntu is skipped due to this error:
    //       "Unable to load shared library 'Photino.Native' or one of its dependencies. In order to help diagnose
    //       loading problems, consider using a tool like strace."
    //   There's probably some way to make it work, but it's not currently a supported Blazor Hybrid scenario anyway
    // - macOS is skipped due to the test not being able to detect when the WebView is ready. There's probably an issue
    //   with the JS code sending a WebMessage to C# and not being sent properly or detected properly.
    [ConditionalFact]
    [OSSkipCondition(OperatingSystems.Linux | OperatingSystems.MacOSX,
        SkipReason = "On Helix/Ubuntu the native Photino assemblies can't be found, and on macOS it can't detect when the WebView is ready")]
    [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/50802")]
    public async Task CanLaunchPhotinoWebViewAndClickButton()
    {
        var photinoTestProgramExePath = typeof(WebViewManagerE2ETests).Assembly.Location;

        // This test launches this very test assembly as an executable so that the Photino UI window
        // can launch and be automated. See the comment in Program.Main() for more info.
        var photinoProcess = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                WorkingDirectory = Path.GetDirectoryName(photinoTestProgramExePath),
                FileName = "dotnet",
                Arguments = $"\"{photinoTestProgramExePath}\"",
                RedirectStandardOutput = true,
            },
        };

        photinoProcess.Start();

        var testProgramOutput = photinoProcess.StandardOutput.ReadToEnd();

        await photinoProcess.WaitForExitAsync().TimeoutAfter(TimeSpan.FromSeconds(30));

        // The test app reports its own results by calling Console.WriteLine(), so here we only need to verify that
        // the test internally believes it passed (and we trust it!).
        Assert.Contains($"Test passed? {true}", testProgramOutput);
    }
}
