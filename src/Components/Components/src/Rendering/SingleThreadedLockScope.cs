// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Components.Rendering;

/// <summary>
/// A disposable lock scope that is a no-op on single-threaded WASM (browser) targets,
/// allowing the trimmer to remove lock overhead.
/// </summary>
/// <remarks>
/// On browser targets, <see cref="OperatingSystem.IsBrowser()"/> is a trimmer-recognized
/// feature switch. Since single-threaded WASM has no concurrent threads, lock/Monitor
/// operations are pure overhead and can safely be skipped.
/// </remarks>
internal ref struct SingleThreadedLockScope
{
    private readonly object _lockObject;
#pragma warning disable IDE0044 // _lockTaken is set by Monitor.Enter via ref parameter
    private bool _lockTaken;
#pragma warning restore IDE0044

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SingleThreadedLockScope(object lockObject)
    {
        _lockObject = lockObject;
        if (!OperatingSystem.IsBrowser())
        {
            Monitor.Enter(lockObject, ref _lockTaken);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        if (_lockTaken)
        {
            Monitor.Exit(_lockObject);
        }
    }
}
