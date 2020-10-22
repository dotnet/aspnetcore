// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;

namespace Microsoft.AspNetCore.Hosting
{
    internal class WebHostLifetime : IDisposable
    {
        private readonly CancellationTokenSource _cts;
        private readonly ManualResetEventSlim _resetEvent;
        private readonly string _shutdownMessage;

        private bool _disposed = false;
        private bool _exitedGracefully = false;

        public WebHostLifetime(CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
        {
            _cts = cts;
            _resetEvent = resetEvent;
            _shutdownMessage = shutdownMessage;

            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            Console.CancelKeyPress += CancelKeyPress;
        }

        internal void SetExitedGracefully()
        {
            _exitedGracefully = true;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            AppDomain.CurrentDomain.ProcessExit -= ProcessExit;
            Console.CancelKeyPress -= CancelKeyPress;
        }

        private void CancelKeyPress(object sender, ConsoleCancelEventArgs eventArgs)
        {
            Shutdown();
            // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
            eventArgs.Cancel = true;
        }

        private void ProcessExit(object sender, EventArgs eventArgs)
        {
            Shutdown();
            if (_exitedGracefully)
            {
                // On Linux if the shutdown is triggered by SIGTERM then that's signaled with the 143 exit code.
                // Suppress that since we shut down gracefully. https://github.com/dotnet/aspnetcore/issues/6526
                Environment.ExitCode = 0;
            }
        }

        private void Shutdown()
        {
            try
            {
                if (!_cts.IsCancellationRequested)
                {
                    if (!string.IsNullOrEmpty(_shutdownMessage))
                    {
                        Console.WriteLine(_shutdownMessage);
                    }
                    _cts.Cancel();
                }
            }
            // When hosting with IIS in-process, we detach the Console handle on main thread exit.
            // Console.WriteLine may throw here as we are logging to console on ProcessExit.
            // We catch and ignore all exceptions here. Do not log to Console in thie exception handler.
            catch (Exception) {}
            // Wait on the given reset event
            _resetEvent.Wait();
        }
    }
}
