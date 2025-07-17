// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DevServerProgram = Microsoft.AspNetCore.Components.WebAssembly.DevServer.Server.Program;

namespace Microsoft.AspNetCore.Components.WebAssembly.DevServer;

internal sealed class Program
{
    static int Main(string[] args)
    {
        var host = DevServerProgram.BuildWebHost(args);

        // Register POSIX signal handlers for graceful shutdown on Unix systems
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            using var lifetime = new DevServerLifetime(host.Services.GetRequiredService<IHostApplicationLifetime>());
            host.Run();
        }
        else
        {
            host.Run();
        }

        return 0;
    }
}

internal sealed class DevServerLifetime : IDisposable
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly PosixSignalRegistration _sigIntRegistration;
    private readonly PosixSignalRegistration _sigQuitRegistration;
    private readonly PosixSignalRegistration _sigTermRegistration;

    private bool _disposed;

    public DevServerLifetime(IHostApplicationLifetime applicationLifetime)
    {
        _applicationLifetime = applicationLifetime;

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
        // Request graceful shutdown
        _applicationLifetime.StopApplication();

        // Don't terminate the process immediately, wait for the application to exit gracefully.
        context.Cancel = true;
    }
}
