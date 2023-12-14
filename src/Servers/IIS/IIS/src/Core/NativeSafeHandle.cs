// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

namespace Microsoft.AspNetCore.Server.IIS.Core;

internal sealed class NativeSafeHandle : SafeHandle, IValueTaskSource<object?>
{
    private ManualResetValueTaskSourceCore<object?> _core; // mutable struct; do not make this readonly

    public override bool IsInvalid => handle == IntPtr.Zero;
    public short Version => _core.Version;

    public NativeSafeHandle(IntPtr handle) : base(IntPtr.Zero, ownsHandle: true)
    {
        this.handle = handle;
    }

    protected override bool ReleaseHandle()
    {
        handle = IntPtr.Zero;

        // Complete the ManualResetValueTaskSourceCore
        if (_core.GetStatus(_core.Version) == ValueTaskSourceStatus.Pending)
        {
            _core.SetResult(null);
        }

        return true;
    }

    public object? GetResult(short token)
    {
        return _core.GetResult(token);
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _core.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _core.OnCompleted(continuation, state, token, flags);
    }
}
