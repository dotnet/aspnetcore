// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.HttpSys.Internal;

internal static class NclUtilities
{
    internal static bool HasShutdownStarted
    {
        get
        {
            return Environment.HasShutdownStarted
                || AppDomain.CurrentDomain.IsFinalizingForUnload();
        }
    }
}
