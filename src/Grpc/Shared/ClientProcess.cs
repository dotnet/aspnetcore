// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Internal;
using Xunit.Abstractions;

namespace InteropTests.Helpers;

public class ClientProcess : IDisposable
{
    private readonly Process _process;
    private readonly ProcessEx _processEx;
    private readonly TaskCompletionSource _startTcs;
    private readonly StringBuilder _output;
    private readonly object _outputLock = new object();

    public ClientProcess(ITestOutputHelper output, string fileName, string arguments)
    {
        _output = new StringBuilder();

        _process = new Process();
        _process.StartInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            FileName = fileName,
            Arguments = arguments
        };
        _process.EnableRaisingEvents = true;
        _process.OutputDataReceived += Process_OutputDataReceived;
        _process.ErrorDataReceived += Process_ErrorDataReceived;
        _process.Start();

        _processEx = new ProcessEx(output, _process, timeout: Timeout.InfiniteTimeSpan);

        _startTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task WaitForReadyAsync() => _startTcs.Task;
    public Task WaitForExitAsync() => _processEx.Exited;
    public int ExitCode => _process.ExitCode;
    public bool IsReady => _startTcs.Task.IsCompletedSuccessfully;

    public string GetOutput()
    {
        lock (_outputLock)
        {
            return _output.ToString();
        }
    }

    private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        var data = e.Data;
        if (data != null)
        {
            if (data.Contains("Application started."))
            {
                _startTcs.TrySetResult();
            }

            lock (_outputLock)
            {
                _output.AppendLine(data);
            }
        }
    }

    private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        var data = e.Data;
        if (data != null)
        {
            lock (_outputLock)
            {
                _output.AppendLine("ERROR: " + data);
            }
        }
    }

    public void Dispose()
    {
        _processEx.Dispose();
    }
}

public static class Publisher
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
            var exitCode = processEx.HasExited ? (int?)processEx.ExitCode : null;

            processEx.Dispose();

            if (exitCode != null && exitCode.Value != 0)
            {
                throw new Exception($"Non-zero exit code returned: {exitCode}");
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
