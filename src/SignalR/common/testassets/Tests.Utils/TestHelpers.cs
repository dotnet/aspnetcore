// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.WebSockets;

namespace Microsoft.AspNetCore.SignalR.Tests
{
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
}
