// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Net.Quic
{
    internal static partial class SR
    {
        // The resource generator used in AspNetCore does not create this method. This file fills in that functional gap
        // so we don't have to modify the shared source.
        internal static string Format(string resourceFormat, params object[] args)
        {
            if (args != null)
            {
                return string.Format(resourceFormat, args);
            }

            return resourceFormat;
        }
    }
}
