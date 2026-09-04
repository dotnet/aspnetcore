// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Certificates.Generation;

namespace Microsoft.AspNetCore.Internal.Tests;

public class CertificateManagerProcessRunnerTests
{
    [Fact]
    public async Task Run_DrainsRedirectedStreamsWithoutDeadlock()
    {
        var startInfo = CreateShellProcessStartInfo(GetLargeOutputCommand());
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        var runTask = Task.Run(() => CertificateManagerProcessRunner.Run(startInfo));
        var completedTask = await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(30)));
        Assert.Same(runTask, completedTask);

        var result = await runTask;
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("out1", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("err1", result.StandardError, StringComparison.Ordinal);
    }

    [Fact]
    public void Run_CapturesRedirectedOutput()
    {
        var startInfo = CreateShellProcessStartInfo(GetSmallOutputCommand());
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        var result = CertificateManagerProcessRunner.Run(startInfo);

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("stdout-ok", result.StandardOutput, StringComparison.Ordinal);
        Assert.Contains("stderr-ok", result.StandardError, StringComparison.Ordinal);
    }

    private static ProcessStartInfo CreateShellProcessStartInfo(string command) => OperatingSystem.IsWindows()
        ? new ProcessStartInfo("cmd.exe", $"/d /c \"{command}\"")
        : new ProcessStartInfo("/bin/sh", $"-c \"{command}\"");

    private static string GetLargeOutputCommand() => OperatingSystem.IsWindows()
        ? "for /L %i in (1,1,6000) do @echo out%i & @echo err%i 1>&2"
        : "i=1; while [ $i -le 6000 ]; do echo out$i; echo err$i 1>&2; i=$((i+1)); done";

    private static string GetSmallOutputCommand() => OperatingSystem.IsWindows()
        ? "echo stdout-ok & echo stderr-ok 1>&2"
        : "echo stdout-ok; echo stderr-ok 1>&2";
}
