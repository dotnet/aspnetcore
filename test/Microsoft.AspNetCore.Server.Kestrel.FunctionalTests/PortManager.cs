// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public static class PortManager
    {
        private static int _nextPort = 8001;

        public static int GetPort()
        {
            return Interlocked.Increment(ref _nextPort);
        }
    }
}
