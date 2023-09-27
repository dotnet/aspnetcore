// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Windows.Win32;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed class HttpServerSessionHandle : CriticalHandleZeroOrMinusOneIsInvalid
{
    private int disposed;
    private readonly ulong serverSessionId;

    internal HttpServerSessionHandle(ulong id)
        : base()
    {
        serverSessionId = id;

        // This class uses no real handle so we need to set a dummy handle. Otherwise, IsInvalid always remains
        // true.

        SetHandle(new IntPtr(1));
    }

    internal ulong DangerousGetServerSessionId()
    {
        return serverSessionId;
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid)
        {
            if (Interlocked.Increment(ref disposed) == 1)
            {
                // Closing server session also closes all open url groups under that server session.
                return PInvoke.HttpCloseServerSession(serverSessionId) ==
                    ErrorCodes.ERROR_SUCCESS;
            }
        }
        return true;
    }
}
