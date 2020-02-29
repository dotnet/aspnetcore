// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Blazor.Server.AutoRebuild
{
    /// <summary>
    /// Finds the VS process that launched this app process (if any), and uses
    /// named pipes to communicate with its AutoRebuild listener (if any).
    /// </summary>
    internal class VSForWindowsRebuildService : IRebuildService
    {
        private const int _connectionTimeoutMilliseconds = 3000;
        private readonly Process _vsProcess;

        public static bool TryCreate(out VSForWindowsRebuildService result)
        {
            var vsProcess = FindAncestorVSProcess();
            if (vsProcess != null)
            {
                result = new VSForWindowsRebuildService(vsProcess);
                return true;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public async Task<bool> PerformRebuildAsync(string projectFullPath, DateTime ifNotBuiltSince)
        {
            var pipeName = $"BlazorAutoRebuild\\{_vsProcess.Id}";
            using (var pipeClient = new NamedPipeClientStream(pipeName))
            {
                await pipeClient.ConnectAsync(_connectionTimeoutMilliseconds);

                // Protocol:
                //   1. Receive protocol version number from the VS listener
                //      If we're incompatible with it, send back special string "abort" and end
                //   2. Send the project path to the VS listener
                //   3. Send the 'if not rebuilt since' timestamp to the VS listener
                //   4. Wait for it to send back a bool representing the result
                // Keep in sync with AutoRebuildService.cs in the BlazorExtension project
                // In the future we may extend this to getting back build error details
                var remoteProtocolVersion = await pipeClient.ReadIntAsync();
                if (remoteProtocolVersion == 1)
                {
                    await pipeClient.WriteStringAsync(projectFullPath);
                    await pipeClient.WriteDateTimeAsync(ifNotBuiltSince);
                    return await pipeClient.ReadBoolAsync();
                }
                else
                {
                    await pipeClient.WriteStringAsync("abort");
                    return false;
                }
            }
        }

        private VSForWindowsRebuildService(Process vsProcess)
        {
            _vsProcess = vsProcess ?? throw new ArgumentNullException(nameof(vsProcess));
        }

        private static Process FindAncestorVSProcess()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return null;
            }

            var candidateProcess = Process.GetCurrentProcess();
            try
            {
                while (candidateProcess != null && !candidateProcess.HasExited)
                {
                    // It's unlikely that anyone's going to have a non-VS process in the process
                    // hierarchy called 'devenv', but if that turns out to be a scenario, we could
                    // (for example) write the VS PID to the obj directory during build, and then
                    // only consider processes with that ID. We still want to be sure there really
                    // is such a process in our ancestor chain, otherwise if you did "dotnet run"
                    // in a command prompt, we'd be confused and think it was launched from VS.
                    if (candidateProcess.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase))
                    {
                        return candidateProcess;
                    }

                    candidateProcess = ProcessUtils.GetParent(candidateProcess);
                }
            }
            catch (Exception)
            {
                // There's probably some permissions issue that prevents us from seeing
                // further up the ancestor list, so we have to stop looking here.
            }

            return null;
        }
    }
}
