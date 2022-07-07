// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Xunit.Abstractions;

namespace Grpc.Tests.Shared;

public static class AppPublisher
{
    public static async Task PublishAppAsync(ITestOutputHelper output, string workingDirectory, string path, string outputPath, bool enableTrimming = false)
    {
        var resolvedPath = Path.GetFullPath(path);
        output.WriteLine($"Publishing {resolvedPath}");

        ProcessEx processEx = null;
        try
        {
#if DEBUG
            var configuration = "Debug";
#else
            var configuration = "Release";
#endif
            var arguments = $"publish {resolvedPath} -r {GetRuntimeIdentifier()} -c {configuration} -o {outputPath} --self-contained";
            if (enableTrimming)
            {
                arguments += " -p:PublishTrimmed=true -p:TrimmerSingleWarn=false -p:ILLinkTreatWarningsAsErrors=false";
            }

            processEx = ProcessEx.Run(
                output,
                workingDirectory,
                "dotnet",
                arguments,
                timeout: TimeSpan.FromSeconds(30));

            await processEx.Exited;
        }
        catch (Exception ex)
        {
            throw new Exception("Error while publishing app.", ex);
        }
        finally
        {
            if (processEx != null)
            {
                var exitCode = processEx.HasExited ? (int?)processEx.ExitCode : null;

                processEx.Dispose();

                if (exitCode != null && exitCode.Value != 0)
                {
                    throw new Exception($"Non-zero exit code returned: {exitCode}");
                }
            }
        }
    }

    private static string GetRuntimeIdentifier()
    {
        var architecture = RuntimeInformation.OSArchitecture.ToString().ToLower(CultureInfo.InvariantCulture);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "win10-" + architecture;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "linux-" + architecture;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "osx-" + architecture;
        }
        throw new InvalidOperationException("Unrecognized operation system platform");
    }
}
