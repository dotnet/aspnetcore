// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using Xunit;
using Xunit.Abstractions;

namespace Templates.Test.Helpers
{
    public class AspNetProcess : IDisposable
    {
        private const string DefaultFramework = "netcoreapp3.0";
        private const string ListeningMessagePrefix = "Now listening on: ";

        private readonly ProcessEx _process;

        public AspNetProcess(
            ITestOutputHelper output,
            string workingDirectory,
            string projectName,
            bool publish,
            int httpPort,
            int httpsPort)
        {
            var now = DateTimeOffset.Now;

            if (publish)
            {
                output.WriteLine("Publishing ASP.NET application...");

                // Workaround for issue with runtime store not yet being published
                // https://github.com/aspnet/Home/issues/2254#issuecomment-339709628
                var extraArgs = "-p:PublishWithAspNetCoreTargetManifest=false";

                ProcessEx
                    .Run(output, workingDirectory, DotNetMuxer.MuxerPathOrDefault(), $"publish -c Release {extraArgs}")
                    .WaitForExit(assertSuccess: true);
                workingDirectory = Path.Combine(workingDirectory, "bin", "Release", DefaultFramework, "publish");
            }
            else
            {
                output.WriteLine("Building ASP.NET application...");
                ProcessEx
                    .Run(output, workingDirectory, DotNetMuxer.MuxerPathOrDefault(), $"build --no-restore -c Debug")
                    .WaitForExit(assertSuccess: true);
            }

            var envVars = new Dictionary<string, string>
            {
                { "ASPNETCORE_URLS", $"http://127.0.0.1:{httpPort};https://127.0.0.1:{httpsPort}" }
            };

            if (!publish)
            {
                envVars["ASPNETCORE_ENVIRONMENT"] = "Development";
            }

            output.WriteLine("Running ASP.NET application...");
            var dllPath = publish ? $"{projectName}.dll" : $"bin/Debug/{DefaultFramework}/{projectName}.dll";
            _process = ProcessEx.Run(output, workingDirectory, DotNetMuxer.MuxerPathOrDefault(), $"exec {dllPath}", envVars: envVars);
        }

        public void Dispose()
        {
            _process.Dispose();
        }
    }
}
