// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Package = Microsoft.VisualStudio.Shell.Package;
using ThreadHelper = Microsoft.VisualStudio.Shell.ThreadHelper;

namespace Microsoft.VisualStudio.BlazorExtension
{
    /// <summary>
    /// The counterpart to VSForWindowsRebuildService.cs in the Blazor.Server project.
    /// Listens for named pipe connections and rebuilds projects on request.
    /// </summary>
    internal class AutoRebuildService
    {
        private const int _protocolVersion = 1;
        private readonly BuildEventsWatcher _buildEventsWatcher;
        private readonly string _pipeName;

        public AutoRebuildService(BuildEventsWatcher buildEventsWatcher)
        {
            _buildEventsWatcher = buildEventsWatcher ?? throw new ArgumentNullException(nameof(buildEventsWatcher));
            _pipeName = $"BlazorAutoRebuild\\{Process.GetCurrentProcess().Id}";
        }

        public void Listen()
        {
            AddBuildServiceNamedPipeServer();
        }

        private void AddBuildServiceNamedPipeServer()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    var identity = WindowsIdentity.GetCurrent();
                    var identifier = identity.Owner;
                    var security = new PipeSecurity();

                    // Restrict access to just this account.
                    var rule = new PipeAccessRule(identifier, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow);
                    security.AddAccessRule(rule);
                    security.SetOwner(identifier);

                    // And our current elevation level
                    var principal = new WindowsPrincipal(identity);
                    var isServerElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);

                    using (var serverPipe = new NamedPipeServerStream(
                        _pipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous | PipeOptions.WriteThrough,
                        0x10000, // 64k input buffer
                        0x10000, // 64k output buffer
                        security,
                        HandleInheritability.None))
                    {
                        // As soon as we receive a connection, spin up another background
                        // listener to wait for the next connection
                        await serverPipe.WaitForConnectionAsync();
                        AddBuildServiceNamedPipeServer();

                        await HandleRequestAsync(serverPipe, isServerElevated);
                    }
                }
                catch (Exception ex)
                {
                    await AttemptLogErrorAsync(
                        $"Error in Blazor AutoRebuildService:\n{ex.Message}\n{ex.StackTrace}");
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private async Task HandleRequestAsync(NamedPipeServerStream stream, bool isServerElevated)
        {
            // Protocol:
            //   1. Send a "protocol version" number to the client
            //   2. Receive the project path from the client
            //      If it is the special string "abort", gracefully disconnect and end
            //      This is to allow for mismatches between server and client protocol version
            //   3. Receive the "if not built since" timestamp from the client
            //   4. Perform the build, then send back the success/failure result flag
            // Keep in sync with VSForWindowsRebuildService.cs in the Blazor.Server project
            // In the future we may extend this to getting back build error details
            await stream.WriteIntAsync(_protocolVersion);
            var projectPath = await stream.ReadStringAsync();

            // We can't do the security check for elevation until we read from the stream.
            if (isServerElevated != IsClientElevated(stream))
            {
                return;
            }

            if (projectPath.Equals("abort", StringComparison.Ordinal))
            {
                return;
            }

            var allowExistingBuildsSince = await stream.ReadDateTimeAsync();
            var buildResult = await _buildEventsWatcher.PerformBuildAsync(projectPath, allowExistingBuildsSince);
            await stream.WriteBoolAsync(buildResult);
        }

        private async Task AttemptLogErrorAsync(string message)
        {
            if (!ThreadHelper.CheckAccess())
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            }

            var outputWindow = (IVsOutputWindow)Package.GetGlobalService(typeof(SVsOutputWindow));
            if (outputWindow != null)
            {
                outputWindow.GetPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, out var pane);
                if (pane != null)
                {
                    pane.OutputString(message);
                    pane.Activate();
                }
            }
        }

        private bool? IsClientElevated(NamedPipeServerStream stream)
        {
            bool? isClientElevated = null;
            stream.RunAsClient(() => 
            {
                var identity = WindowsIdentity.GetCurrent(ifImpersonating: true);
                var principal = new WindowsPrincipal(identity);
                isClientElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
            });

            return isClientElevated;
        }
    }
}
