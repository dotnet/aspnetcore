// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.DirectSsl;

/// <summary>
/// Pool of SSL event pumps. Each pump owns a set of connections and handles
/// all SSL I/O for those connections on a dedicated thread.
/// </summary>
internal sealed class SslEventPumpPool : IDisposable
{
    private readonly SslEventPump[] _pumps;
    private int _nextPump;

    public SslEventPumpPool(int pumpCount = 0, ILoggerFactory? loggerFactory = null)
    {
        // Default: 1 pump per CPU core, like nginx
        pumpCount = pumpCount > 0 ? pumpCount : Environment.ProcessorCount;

        _pumps = new SslEventPump[pumpCount];
        for (int i = 0; i < pumpCount; i++)
        {
            _pumps[i] = new SslEventPump(loggerFactory?.CreateLogger<SslEventPump>(), i);
        }
    }

    /// <summary>
    /// Returns the next pump in a round-robin fashion.
    /// </summary>
    public SslEventPump GetNextPump()
    {
        int idx = Interlocked.Increment(ref _nextPump) % _pumps.Length;
        return _pumps[idx];
    }

    public void Dispose()
    {
        foreach (var pump in _pumps)
        {
            pump.Dispose();
        }
    }
}