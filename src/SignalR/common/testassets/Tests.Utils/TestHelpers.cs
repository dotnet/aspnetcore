// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.AspNetCore.SignalR.Tests
{
	public static class TestHelpers
    {
        public static bool IsWebSocketsSupported()
        {
#if NETCOREAPP3_0
            // .NET Core 2.1 and greater has sockets
            return true;
#else
                // Non-Windows platforms have sockets
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return true;
                }

                // Windows 8 and greater has sockets
                return Environment.OSVersion.Version >= new Version(6, 2);
#endif
        }
    }
}
