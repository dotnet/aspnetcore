// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal static class ServerConnection
    {
        private const string ServerName = "rzc.dll";

        // Spend up to 1s connecting to existing process (existing processes should be always responsive).
        private const int TimeOutMsExistingProcess = 1000;

        // Spend up to 20s connecting to a new process, to allow time for it to start.
        private const int TimeOutMsNewProcess = 20000;

        // Custom delegate that contains an out param to use with TryCreateServerCore method.
        private delegate TResult TryCreateServerCoreDelegate<T1, T2, T3, T4, out TResult>(T1 arg1, T2 arg2, out T3 arg3, T4 arg4);

        public static bool WasServerMutexOpen(string mutexName)
        {
            Mutex mutex = null;
            var open = false;
            try
            {
                open = Mutex.TryOpenExisting(mutexName, out mutex);
            }
            catch
            {
                // In the case an exception occurred trying to open the Mutex then
                // the assumption is that it's not open.
            }

            mutex?.Dispose();

            return open;
        }

        /// <summary>
        /// Gets the value of the temporary path for the current environment assuming the working directory
        /// is <paramref name="workingDir"/>.  This function must emulate <see cref="Path.GetTempPath"/> as 
        /// closely as possible.
        /// </summary>
        public static string GetTempPath(string workingDir)
        {
            if (PlatformInformation.IsUnix)
            {
                // Unix temp path is fine: it does not use the working directory
                // (it uses ${TMPDIR} if set, otherwise, it returns /tmp)
                return Path.GetTempPath();
            }

            var tmp = Environment.GetEnvironmentVariable("TMP");
            if (Path.IsPathRooted(tmp))
            {
                return tmp;
            }

            var temp = Environment.GetEnvironmentVariable("TEMP");
            if (Path.IsPathRooted(temp))
            {
                return temp;
            }

            if (!string.IsNullOrEmpty(workingDir))
            {
                if (!string.IsNullOrEmpty(tmp))
                {
                    return Path.Combine(workingDir, tmp);
                }

                if (!string.IsNullOrEmpty(temp))
                {
                    return Path.Combine(workingDir, temp);
                }
            }

            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (Path.IsPathRooted(userProfile))
            {
                return userProfile;
            }

            return Environment.GetEnvironmentVariable("SYSTEMROOT");
        }

        public static Task<ServerResponse> RunOnServer(
            string pipeName,
            IList<string> arguments,
            ServerPaths serverPaths,
            CancellationToken cancellationToken,
            string keepAlive = null,
            bool debug = false)
        {
            if (string.IsNullOrEmpty(pipeName))
            {
                pipeName = PipeName.ComputeDefault(serverPaths.ClientDirectory);
            }

            return RunOnServerCore(
                arguments,
                serverPaths,
                pipeName: pipeName,
                keepAlive: keepAlive,
                timeoutOverride: null,
                tryCreateServerFunc: TryCreateServerCore,
                cancellationToken: cancellationToken,
                debug: debug);
        }

        private static async Task<ServerResponse> RunOnServerCore(
            IList<string> arguments,
            ServerPaths serverPaths,
            string pipeName,
            string keepAlive,
            int? timeoutOverride,
            TryCreateServerCoreDelegate<string, string, int?, bool, bool> tryCreateServerFunc,
            CancellationToken cancellationToken,
            bool debug)
        {
            if (pipeName == null)
            {
                return new RejectedServerResponse();
            }

            if (serverPaths.TempDirectory == null)
            {
                return new RejectedServerResponse();
            }

            var clientDir = serverPaths.ClientDirectory;
            var timeoutNewProcess = timeoutOverride ?? TimeOutMsNewProcess;
            var timeoutExistingProcess = timeoutOverride ?? TimeOutMsExistingProcess;
            var clientMutexName = MutexName.GetClientMutexName(pipeName);
            Task<Client> pipeTask = null;

            Mutex clientMutex = null;
            var holdsMutex = false;

            try
            {
                try
                {
                    clientMutex = new Mutex(initiallyOwned: true, name: clientMutexName, createdNew: out holdsMutex);
                }
                catch (Exception ex)
                {
                    // The Mutex constructor can throw in certain cases. One specific example is docker containers
                    // where the /tmp directory is restricted. In those cases there is no reliable way to execute
                    // the server and we need to fall back to the command line.
                    // Example: https://github.com/dotnet/roslyn/issues/24124

                    ServerLogger.LogException(ex, "Client mutex creation failed.");

                    return new RejectedServerResponse();
                }

                if (!holdsMutex)
                {
                    try
                    {
                        holdsMutex = clientMutex.WaitOne(timeoutNewProcess);

                        if (!holdsMutex)
                        {
                            return new RejectedServerResponse();
                        }
                    }
                    catch (AbandonedMutexException)
                    {
                        holdsMutex = true;
                    }
                }

                // Check for an already running server
                var serverMutexName = MutexName.GetServerMutexName(pipeName);
                var wasServerRunning = WasServerMutexOpen(serverMutexName);
                var timeout = wasServerRunning ? timeoutExistingProcess : timeoutNewProcess;

                if (wasServerRunning || tryCreateServerFunc(clientDir, pipeName, out var _, debug))
                {
                    pipeTask = Client.ConnectAsync(pipeName, TimeSpan.FromMilliseconds(timeout), cancellationToken);
                }
            }
            finally
            {
                if (holdsMutex)
                {
                    clientMutex?.ReleaseMutex();
                }

                clientMutex?.Dispose();
            }

            if (pipeTask != null)
            {
                var client = await pipeTask.ConfigureAwait(false);
                if (client != null)
                {
                    var request = ServerRequest.Create(
                        serverPaths.WorkingDirectory,
                        serverPaths.TempDirectory,
                        arguments,
                        keepAlive);

                    return await TryProcessRequest(client, request, cancellationToken).ConfigureAwait(false);
                }
            }

            return new RejectedServerResponse();
        }

        /// <summary>
        /// Try to process the request using the server. Returns a null-containing Task if a response
        /// from the server cannot be retrieved.
        /// </summary>
        private static async Task<ServerResponse> TryProcessRequest(
            Client client,
            ServerRequest request,
            CancellationToken cancellationToken)
        {
            ServerResponse response;
            using (client)
            {
                // Write the request
                try
                {
                    ServerLogger.Log("Begin writing request");
                    await request.WriteAsync(client.Stream, cancellationToken).ConfigureAwait(false);
                    ServerLogger.Log("End writing request");
                }
                catch (Exception e)
                {
                    ServerLogger.LogException(e, "Error writing build request.");
                    return new RejectedServerResponse();
                }

                // Wait for the compilation and a monitor to detect if the server disconnects
                var serverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                ServerLogger.Log("Begin reading response");

                var responseTask = ServerResponse.ReadAsync(client.Stream, serverCts.Token);
                var monitorTask = client.WaitForDisconnectAsync(serverCts.Token);
                await Task.WhenAny(responseTask, monitorTask).ConfigureAwait(false);

                ServerLogger.Log("End reading response");

                if (responseTask.IsCompleted)
                {
                    // await the task to log any exceptions
                    try
                    {
                        response = await responseTask.ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        ServerLogger.LogException(e, "Error reading response");
                        response = new RejectedServerResponse();
                    }
                }
                else
                {
                    ServerLogger.Log("Server disconnect");
                    response = new RejectedServerResponse();
                }

                // Cancel whatever task is still around
                serverCts.Cancel();
                Debug.Assert(response != null);
                return response;
            }
        }

        // Internal for testing.
        internal static bool TryCreateServerCore(string clientDir, string pipeName, out int? processId, bool debug = false)
        {
            processId = null;

            // The server should be in the same directory as the client
            var expectedCompilerPath = Path.Combine(clientDir, ServerName);
            var expectedPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
            var argumentList = new string[]
            {
                expectedCompilerPath,
                debug ? "--debug" : "",
                "server",
                "-p",
                pipeName
            };
            var processArguments = ArgumentEscaper.EscapeAndConcatenate(argumentList);

            if (!File.Exists(expectedCompilerPath))
            {
                return false;
            }

            if (PlatformInformation.IsWindows)
            {
                // Currently, there isn't a way to use the Process class to create a process without
                // inheriting handles(stdin/stdout/stderr) from its parent. This might cause the parent process
                // to block on those handles. So we use P/Invoke. This code was taken from MSBuild task starting code.
                // The work to customize this behavior is being tracked by https://github.com/dotnet/corefx/issues/306.

                var startInfo = new STARTUPINFO();
                startInfo.cb = Marshal.SizeOf(startInfo);
                startInfo.hStdError = NativeMethods.InvalidIntPtr;
                startInfo.hStdInput = NativeMethods.InvalidIntPtr;
                startInfo.hStdOutput = NativeMethods.InvalidIntPtr;
                startInfo.dwFlags = NativeMethods.STARTF_USESTDHANDLES;
                var dwCreationFlags = NativeMethods.NORMAL_PRIORITY_CLASS | NativeMethods.CREATE_NO_WINDOW;

                ServerLogger.Log("Attempting to create process '{0}'", expectedPath);

                var builder = new StringBuilder($@"""{expectedPath}"" {processArguments}");

                var success = NativeMethods.CreateProcess(
                    lpApplicationName: null,
                    lpCommandLine: builder,
                    lpProcessAttributes: NativeMethods.NullPtr,
                    lpThreadAttributes: NativeMethods.NullPtr,
                    bInheritHandles: false,
                    dwCreationFlags: dwCreationFlags,
                    lpEnvironment: NativeMethods.NullPtr, // Inherit environment
                    lpCurrentDirectory: clientDir,
                    lpStartupInfo: ref startInfo,
                    lpProcessInformation: out var processInfo);

                if (success)
                {
                    ServerLogger.Log("Successfully created process with process id {0}", processInfo.dwProcessId);
                    NativeMethods.CloseHandle(processInfo.hProcess);
                    NativeMethods.CloseHandle(processInfo.hThread);
                    processId = processInfo.dwProcessId;
                }
                else
                {
                    ServerLogger.Log("Failed to create process. GetLastError={0}", Marshal.GetLastWin32Error());
                }
                return success;
            }
            else
            {
                try
                {
                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = expectedPath,
                        Arguments = processArguments,
                        UseShellExecute = false,
                        WorkingDirectory = clientDir,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var process = Process.Start(startInfo);
                    processId = process.Id;

                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// This class provides simple properties for determining whether the current platform is Windows or Unix-based.
    /// We intentionally do not use System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(...) because
    /// it incorrectly reports 'true' for 'Windows' in desktop builds running on Unix-based platforms via Mono.
    /// </summary>
    internal static class PlatformInformation
    {
        public static bool IsWindows => Path.DirectorySeparatorChar == '\\';
        public static bool IsUnix => Path.DirectorySeparatorChar == '/';
    }
}
