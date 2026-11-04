// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.Hosting;

internal sealed class WebHostLifetime : IDisposable
{
    private readonly CancellationTokenSource _cts;
    private readonly ManualResetEventSlim _resetEvent;
    private readonly string _shutdownMessage;

    private readonly PosixSignalRegistration _sigIntRegistration;
    private readonly PosixSignalRegistration _sigQuitRegistration;
    private readonly PosixSignalRegistration _sigTermRegistration;

    private bool _disposed;

    public WebHostLifetime(CancellationTokenSource cts, ManualResetEventSlim resetEvent, string shutdownMessage)
    {
        _cts = cts;
        _resetEvent = resetEvent;
        _shutdownMessage = shutdownMessage;

        Action<PosixSignalContext> handler = HandlePosixSignal;
        _sigIntRegistration = PosixSignalRegistration.Create(PosixSignal.SIGINT, handler);
        _sigQuitRegistration = PosixSignalRegistration.Create(PosixSignal.SIGQUIT, handler);
        _sigTermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, handler);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _sigIntRegistration.Dispose();
        _sigQuitRegistration.Dispose();
        _sigTermRegistration.Dispose();
    }

    private void HandlePosixSignal(PosixSignalContext context)
    {
        Shutdown();

        // Don't terminate the process immediately, wait for the Main thread to exit gracefully.
        context.Cancel = true;
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
        // We catch and ignore all exceptions here. Do not log to Console in this exception handler.
        catch (Exception) { }
        // Wait on the given reset event
        _resetEvent.Wait();
    }
}
