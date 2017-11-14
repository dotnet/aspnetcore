// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.SignalR.Redis.Tests
{
    public class Docker
    {
        private static readonly string _exeSuffix = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : string.Empty;

        private static readonly string _dockerContainerName = "redisTestContainer";
        private static Lazy<Docker> _instance = new Lazy<Docker>(Create);

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

        public int Start(ILogger logger)
        {
            logger.LogInformation("Starting docker container");

            // create and run docker container, remove automatically when stopped, map 6379 from the container to 6379 localhost
            // use static name 'redisTestContainer' so if the container doesn't get removed we don't keep adding more
            // use redis base docker image
            return RunProcess(_path, $"run --rm -p 6379:6379 --name {_dockerContainerName} -d redis", logger);
        }

        public int Stop(ILogger logger)
        {
            logger.LogInformation("Stopping docker container");
            return RunProcess(_path, $"stop {_dockerContainerName}", logger);
        }

        private static int RunProcess(string fileName, string arugments, ILogger logger)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arugments,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                },
                EnableRaisingEvents = true
            };

            var exitCode = 0;
            process.Exited += (_, __) => exitCode = process.ExitCode;
            process.OutputDataReceived += (_, a) => LogIfNotNull(logger.LogInformation, "stdout: {0}", a.Data);
            process.ErrorDataReceived += (_, a) => LogIfNotNull(logger.LogError, "stderr: {0}", a.Data);

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            process.WaitForExit(5000);

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