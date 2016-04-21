// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading;

namespace Microsoft.AspNetCore.Server.Kestrel.FunctionalTests
{
    public static class PortManager
    {
        public static int GetPort()
        {
            return TestCommon.PortManager.GetNextPort();
        }
    }
}
