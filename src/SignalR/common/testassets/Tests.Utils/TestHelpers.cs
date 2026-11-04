// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.WebSockets;

namespace Microsoft.AspNetCore.SignalR.Tests;

public static class TestHelpers
{
    public static bool IsWebSocketsSupported()
    {
        try
        {
            new ClientWebSocket().Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
