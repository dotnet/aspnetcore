// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Internal;

namespace Microsoft.Net.Http.Server
{
    internal static class ComNetOS
    {
        // Minimum support for Windows 7 is assumed.
        internal static readonly bool IsWin8orLater;

        static ComNetOS()
        {
            var win8Version = new Version(6, 2);

#if NETSTANDARD1_3
            IsWin8orLater = (new Version(RuntimeEnvironment.OperatingSystemVersion) >= win8Version);
#else
            IsWin8orLater = (Environment.OSVersion.Version >= win8Version);
#endif
        }
    }
}
