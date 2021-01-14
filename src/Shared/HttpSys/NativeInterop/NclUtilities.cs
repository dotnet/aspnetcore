// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

// Remove once HttpSys has enabled nullable
#nullable enable

namespace Microsoft.AspNetCore.HttpSys.Internal
{
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
}
