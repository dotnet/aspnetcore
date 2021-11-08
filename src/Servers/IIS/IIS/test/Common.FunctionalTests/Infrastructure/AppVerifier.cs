// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNetCore.Server.IntegrationTesting;

public class AppVerifier
{
    private static readonly TimeSpan AppVerifierCommandTimeout = TimeSpan.FromSeconds(5);

    public static IDisposable Disable(ServerType serverType, int code)
    {
        // Set in SetupTestEnvironment.ps1
        var enabledCodes = (Environment.GetEnvironmentVariable("APPVERIFIER_ENABLED_CODES") ?? "").Split(' ');
        string processName;
        switch (serverType)
        {
            case ServerType.IISExpress:
                processName = "iisexpress.exe";
                break;
            case ServerType.IIS:
                processName = "w3wp.exe";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(serverType), serverType, null);
        }

        if (!enabledCodes.Contains(code.ToString(CultureInfo.InvariantCulture)))
        {
            return null;
        }

        RunProcessAndWaitForExit("appverif.exe", $"-configure {code} -for {processName} -with ErrorReport=0", AppVerifierCommandTimeout);
        return new AppVerifierToken(processName, code.ToString(CultureInfo.InvariantCulture));
    }

    private static void RunProcessAndWaitForExit(string fileName, string arguments, TimeSpan timeout)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        var process = Process.Start(startInfo);

        if (!process.WaitForExit((int)timeout.TotalMilliseconds))
        {
            process.Kill();
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Exit code {process.ExitCode} when running {fileName} {arguments}. Stdout: {process.StandardOutput.ReadToEnd()} Stderr: {process.StandardError.ReadToEnd()}");
        }
    }

    public class AppVerifierToken : IDisposable
    {
        private readonly string _processName;

        private readonly string _codes;

        public AppVerifierToken(string processName, string codes)
        {
            _processName = processName;
            _codes = codes;
        }

        public void Dispose()
        {
            //
            RunProcessAndWaitForExit("appverif.exe", $"-configure {_codes} -for {_processName} -with ErrorReport={Environment.GetEnvironmentVariable("APPVERIFIER_LEVEL")}", AppVerifierCommandTimeout);
        }
    }
}
