// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
    public class Docker
    {
        private static readonly string _exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        private static readonly string _dockerContainerName = "redisTestContainer";
        private static readonly Lazy<Docker> _instance = new Lazy<Docker>(Create);

        public static Docker Default => _instance.Value;

        private readonly string _path;

        public Docker(string path)
        {
            _path = path;
        }

        private static Docker Create()
        {
            // Currently Windows Server 2016 doesn't support linux containers which redis is.
            if (string.Equals("True", Environment.GetEnvironmentVariable("APPVEYOR"), StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var location = GetDockerLocation();
            return location == null ? null : new Docker(location);
        }

        private static string GetDockerLocation()
        {
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

        public void Start(ILogger logger)
        {
            logger.LogInformation("Starting docker container");

            // stop container if there is one, could be from a previous test run, ignore failures
            RunProcess(_path, $"stop {_dockerContainerName}", logger, TimeSpan.FromSeconds(5), out var output);

            // create and run docker container, remove automatically when stopped, map 6379 from the container to 6379 localhost
            // use static name 'redisTestContainer' so if the container doesn't get removed we don't keep adding more
            // use redis base docker image
            // 20 second timeout to allow redis image to be downloaded, should be a rare occurance, only happening when a new version is released
            RunProcessAndThrowIfFailed(_path, $"run --rm -p 6379:6379 --name {_dockerContainerName} -d redis", logger, TimeSpan.FromSeconds(20));
        }

        public void Stop(ILogger logger)
        {
            logger.LogInformation("Stopping docker container");
            RunProcessAndThrowIfFailed(_path, $"stop {_dockerContainerName}", logger, TimeSpan.FromSeconds(5));
        }

        public int RunCommand(string commandAndArguments, out string output) =>
            RunCommand(commandAndArguments, NullLogger.Instance, out output);

        public int RunCommand(string commandAndArguments, ILogger logger, out string output)
        {
            return RunProcess(_path, commandAndArguments, logger, TimeSpan.FromSeconds(5), out output);
        }

        private static void RunProcessAndThrowIfFailed(string fileName, string arguments, ILogger logger, TimeSpan timeout)
        {
            var exitCode = RunProcess(fileName, arguments, logger, timeout, out var output);

            if (exitCode != 0)
            {
                throw new Exception($"Command '{fileName} {arguments}' failed with exit code '{exitCode}'. Output:{Environment.NewLine}{output}");
            }
        }

        private static int RunProcess(string fileName, string arguments, ILogger logger, TimeSpan timeout, out string output)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                },
                EnableRaisingEvents = true
            };

            var exitCode = 0;
            var lines = new ConcurrentQueue<string>();
            process.Exited += (_, __) => exitCode = process.ExitCode;
            process.OutputDataReceived += (_, a) =>
            {
                LogIfNotNull(logger.LogInformation, "stdout: {0}", a.Data);
                lines.Enqueue(a.Data);
            };
            process.ErrorDataReceived += (_, a) =>
            {
                LogIfNotNull(logger.LogError, "stderr: {0}", a.Data);
                lines.Enqueue(a.Data);
            };

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Close();
                logger.LogError("Closing process '{processName}' because it is running longer than the configured timeout.", fileName);
            }

            output = string.Join(Environment.NewLine, lines);

            return exitCode;
        }

        private static void LogIfNotNull(Action<string, object[]> logger, string message, string data)
        {
            if (!string.IsNullOrEmpty(data))
            {
                logger(message, new[] { data });
            }
        }
    }
}
