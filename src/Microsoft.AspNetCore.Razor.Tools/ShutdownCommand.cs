// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Tools
{
    internal class ShutdownCommand : CommandBase
    {
        public ShutdownCommand(Application parent)
            : base(parent, "shutdown")
        {
            Pipe = Option("-p|--pipe", "name of named pipe", CommandOptionType.SingleValue);
            Wait = Option("-w|--wait", "wait for shutdown", CommandOptionType.NoValue);
        }

        public CommandOption Pipe { get; }

        public CommandOption Wait { get; }

        protected override bool ValidateArguments()
        {
            if (string.IsNullOrEmpty(Pipe.Value()))
            {
                Pipe.Values.Add(PipeName.ComputeDefault());
            }

            return true;
        }

        protected async override Task<int> ExecuteCoreAsync()
        {
            if (!IsServerRunning())
            {
                // server isn't running right now
                Out.Write("Server is not running.");
                return 0;
            }

            try
            {
                using (var client = await Client.ConnectAsync(Pipe.Value(), timeout: null, cancellationToken: Cancelled))
                {
                    var request = ServerRequest.CreateShutdown();
                    await request.WriteAsync(client.Stream, Cancelled).ConfigureAwait(false);

                    var response = ((ShutdownServerResponse)await ServerResponse.ReadAsync(client.Stream, Cancelled));

                    if (Wait.HasValue())
                    {
                        try
                        {
                            var process = Process.GetProcessById(response.ServerProcessId);
                            process.WaitForExit();
                        }
                        catch (Exception ex)
                        {
                            // There is an inherent race here with the server process.  If it has already shutdown
                            // by the time we try to access it then the operation has succeeded.
                            Error.Write(ex);
                        }

                        Out.Write("Server pid:{0} shut down", response.ServerProcessId);
                    }
                }
            }
            catch (Exception ex) when (IsServerRunning())
            {
                // Ignore an exception that occurred while the server was shutting down.
                Error.Write(ex);
            }

            return 0;
        }

        private bool IsServerRunning()
        {
            if (Mutex.TryOpenExisting(MutexName.GetServerMutexName(Pipe.Value()), out var mutex))
            {
                mutex.Dispose();
                return true;
            }

            return false;
        }
    }
}
