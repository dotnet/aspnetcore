// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.StackExchangeRedis.Tests;

public class Docker
{
    private static readonly string _exeSuffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;

    private static readonly string _dockerContainerName = "redisTestContainer";
    private static readonly string _dockerMonitorContainerName = _dockerContainerName + "Monitor";
    private static readonly Lazy<Docker> _instance = new Lazy<Docker>(Create);

    public static Docker Default => _instance.Value;

    private readonly string _path;

    public Docker(string path)
    {
        _path = path;
    }

    private static Docker Create()
    {
        var location = GetDockerLocation();
        if (location == null)
        {
            return null;
        }

        var docker = new Docker(location);

        docker.RunCommand("info --format '{{.OSType}}'", "docker info", out var output);

        if (!string.Equals(output.Trim('\'', '"', '\r', '\n', ' '), "linux"))
        {
            Console.WriteLine($"'docker info' output: {output}");
            return null;
        }

        return docker;
    }

    private static string GetDockerLocation()
    {
        // OSX + Docker + Redis don't play well together for some reason. We already have these tests covered on Linux and Windows
        // So we are happy ignoring them on OSX
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return null;
        }

        foreach (var dir in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(dir, "docker" + _exeSuffix);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private void StartRedis(ILogger logger)
    {
        try
        {
            Run();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting redis docker container, retrying.");
            Thread.Sleep(1000);

            // Call stop just in case the container somehow started after the timeout so our retry logic doesn't fail
            RunProcessAndWait(_path, $"stop {_dockerContainerName}", "docker stop", logger, TimeSpan.FromSeconds(15), out var _);
            Run();
        }

        var started = false;
        var wait = TimeSpan.FromSeconds(30);
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        while (stopWatch.Elapsed < wait)
        {
            RunProcessAndWait(_path, $"logs {_dockerContainerName}", "docker logs", logger, TimeSpan.FromSeconds(5), out var logOutput);
            if (logOutput.Contains("Ready to accept connections"))
            {
                started = true;
                break;
            }
        }

        if (!started)
        {
            throw new Exception("Redis took too long to start.");
        }

        void Run()
        {
            // create and run docker container, remove automatically when stopped, map 6379 from the container to 6379 localhost
            // use static name 'redisTestContainer' so if the container doesn't get removed we don't keep adding more
            // use redis base docker image
            // 30 second timeout to allow redis image to be downloaded, should be a rare occurrence, only happening when a new version is released
            RunProcessAndThrowIfFailed(_path, $"run --rm -p 6379:6379 --name {_dockerContainerName} -d redis", "redis", logger, TimeSpan.FromMinutes(1));
        }
    }

    public void Start(ILogger logger)
    {
        logger.LogInformation("Starting docker container");

        // stop container if there is one, could be from a previous test run, ignore failures
        RunProcessAndWait(_path, $"stop {_dockerMonitorContainerName}", "docker stop", logger, TimeSpan.FromSeconds(15), out var _);
        RunProcessAndWait(_path, $"stop {_dockerContainerName}", "docker stop", logger, TimeSpan.FromSeconds(15), out var output);

        StartRedis(logger);

        // inspect the redis docker image and extract the IPAddress. Necessary when running tests from inside a docker container, spinning up a new docker container for redis
        // outside the current container requires linking the networks (difficult to automate) or using the IP:Port combo
        RunProcessAndWait(_path, "inspect --format=\"{{range .NetworkSettings.Networks}}{{.IPAddress}}{{end}}\" " + _dockerContainerName, "docker ipaddress", logger, TimeSpan.FromSeconds(5), out output);
        output = output.Trim().Replace(Environment.NewLine, "");

        // variable used by Startup.cs
        Environment.SetEnvironmentVariable("REDIS_CONNECTION", $"{output}:6379");

        var (monitorProcess, monitorOutput) = RunProcess(_path, $"run -i --name {_dockerMonitorContainerName} --link {_dockerContainerName}:redis --rm redis redis-cli -h redis -p 6379", "redis monitor", logger);
        monitorProcess.StandardInput.WriteLine("MONITOR");
        monitorProcess.StandardInput.Flush();
    }

    public void Stop(ILogger logger)
    {
        // Get logs from Redis container before stopping the container
        RunProcessAndThrowIfFailed(_path, $"logs {_dockerContainerName}", "docker logs", logger, TimeSpan.FromSeconds(5));

        logger.LogInformation("Stopping docker container");
        RunProcessAndWait(_path, $"stop {_dockerMonitorContainerName}", "docker stop", logger, TimeSpan.FromSeconds(15), out var _);
        RunProcessAndWait(_path, $"stop {_dockerContainerName}", "docker stop", logger, TimeSpan.FromSeconds(15), out var _);
    }

    public int RunCommand(string commandAndArguments, string prefix, out string output) =>
        RunCommand(commandAndArguments, prefix, NullLogger.Instance, out output);

    public int RunCommand(string commandAndArguments, string prefix, ILogger logger, out string output)
    {
        return RunProcessAndWait(_path, commandAndArguments, prefix, logger, TimeSpan.FromSeconds(30), out output);
    }

    private static void RunProcessAndThrowIfFailed(string fileName, string arguments, string prefix, ILogger logger, TimeSpan timeout)
    {
        var exitCode = RunProcessAndWait(fileName, arguments, prefix, logger, timeout, out var output);

        if (exitCode != 0)
        {
            throw new Exception($"Command '{fileName} {arguments}' failed with exit code '{exitCode}'. Output:{Environment.NewLine}{output}");
        }
    }

    private static int RunProcessAndWait(string fileName, string arguments, string prefix, ILogger logger, TimeSpan timeout, out string output)
    {
        var (process, lines) = RunProcess(fileName, arguments, prefix, logger);

        using (process)
        {
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Close();
                logger.LogError("Closing process '{processName}' because it is running longer than the configured timeout.", fileName);
                output = string.Join(Environment.NewLine, lines.ToArray());
                return -1;
            }
            else
            {
                // Need to WaitForExit without a timeout to guarantee the output stream has written everything
                process.WaitForExit();
            }

            output = string.Join(Environment.NewLine, lines);

            return process.ExitCode;
        }
    }

    private static (Process, ConcurrentQueue<string>) RunProcess(string fileName, string arguments, string prefix, ILogger logger)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            },
            EnableRaisingEvents = true
        };

        var lines = new ConcurrentQueue<string>();
        process.OutputDataReceived += (_, a) =>
        {
            LogIfNotNull(logger.LogInformation, $"'{prefix}' stdout: {{0}}", a.Data);
            lines.Enqueue(a.Data);
        };
        process.ErrorDataReceived += (_, a) =>
        {
            LogIfNotNull(logger.LogError, $"'{prefix}' stderr: {{0}}", a.Data);
            lines.Enqueue(a.Data);
        };

        process.Start();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        return (process, lines);
    }

    private static void LogIfNotNull(Action<string, object[]> logger, string message, string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            logger(message, new[] { data });
        }
    }
}
