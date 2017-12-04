// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Tests
{
	public static class TestHelpers
    {
        public static bool IsWebSocketsSupported()
        {
            try
            {
                new System.Net.WebSockets.ClientWebSocket().Dispose();
            }
            catch (PlatformNotSupportedException)
            {
                return false;
            }

            return true;
        }
    }
}
