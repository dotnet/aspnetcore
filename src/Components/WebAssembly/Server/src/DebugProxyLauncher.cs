// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    internal static class DebugProxyLauncher
    {
        private static object LaunchLock = new object();
        private static Task<string> LaunchedDebugProxyUrl;

        public static Task<string> EnsureLaunchedAndGetUrl(IServiceProvider serviceProvider)
        {
            lock (LaunchLock)
            {
                if (LaunchedDebugProxyUrl == null)
                {
                    LaunchedDebugProxyUrl = LaunchAndGetUrl(serviceProvider);
                }

                return LaunchedDebugProxyUrl;
            }
        }

        private static async Task<string> LaunchAndGetUrl(IServiceProvider serviceProvider)
        {
            var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
            var executablePath = LocateDebugProxyExecutable(environment);
            var muxerPath = DotNetMuxer.MuxerPath;
            var processStartInfo = new ProcessStartInfo
            {
                FileName = muxerPath,
                Arguments = $"exec \"{executablePath}\" --urls http://localhost:63001",
                UseShellExecute = false,
            };
            RemoveUnwantedEnvironmentVariables(processStartInfo.Environment);
            Process.Start(processStartInfo);

            await Task.Delay(1000);
            return "http://localhost:63001";
        }

        private static void RemoveUnwantedEnvironmentVariables(IDictionary<string, string> environment)
        {
            // Generally we expect to pass through most environment variables, since dotnet might
            // need them for arbitrary reasons to function correctly. However, we specifically don't
            // want to pass through any ASP.NET Core hosting related ones, since the child process
            // shouldn't be trying to use the same port numbers, etc. In particular we need to break
            // the association with IISExpress and the MS-ASPNETCORE-TOKEN check.
            var keysToRemove = environment.Keys.Where(key => key.StartsWith("ASPNETCORE_")).ToList();
            foreach (var key in keysToRemove)
            {
                environment.Remove(key);
            }
        }

        private static string LocateDebugProxyExecutable(IWebHostEnvironment environment)
        {
            var assembly = Assembly.Load(environment.ApplicationName);
            var assemblyLocation = GetAssemblyLocation(assembly);
            var debugProxyPath = Path.Combine(
                Path.GetDirectoryName(assemblyLocation),
                "BlazorDebugProxy",
                "Microsoft.AspNetCore.Components.WebAssembly.DebugProxy.dll");

            if (!File.Exists(debugProxyPath))
            {
                throw new InvalidOperationException(
                    $"Cannot start debug proxy because it cannot be found at '{debugProxyPath}'");
            }

            return debugProxyPath;
        }

        internal static string GetAssemblyLocation(Assembly assembly)
        {
            if (Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out var result) &&
                result.IsFile && string.IsNullOrWhiteSpace(result.Fragment))
            {
                return result.LocalPath;
            }

            return assembly.Location;
        }
    }
}
