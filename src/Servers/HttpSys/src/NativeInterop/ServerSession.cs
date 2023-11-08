// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Windows.Win32;

namespace Microsoft.AspNetCore.Server.HttpSys;

internal sealed class ServerSession : IDisposable
{
    internal unsafe ServerSession()
    {
        ulong serverSessionId = 0;
        var statusCode = PInvoke.HttpCreateServerSession(
            HttpApi.Version, &serverSessionId, 0);

        if (statusCode != ErrorCodes.ERROR_SUCCESS)
        {
            throw new HttpSysException((int)statusCode);
        }

        Debug.Assert(serverSessionId != 0, "Invalid id returned by HttpCreateServerSession");

        Id = new HttpServerSessionHandle(serverSessionId);
    }

    public HttpServerSessionHandle Id { get; private set; }

    public void Dispose()
    {
        Id.Dispose();
    }
}
