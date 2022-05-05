// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;

/// <summary>
/// Summary description for Disposable
/// </summary>
internal sealed class Disposable : IDisposable
{
    private Action? _dispose;
    private bool _disposedValue; // To detect redundant calls

    public Disposable(Action dispose)
    {
        _dispose = dispose;
    }

    void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _dispose!.Invoke();
            }

            _dispose = null;
            _disposedValue = true;
        }
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
