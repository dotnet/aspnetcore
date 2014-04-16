// -----------------------------------------------------------------------
// <copyright file="ComNetOS.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Microsoft.Net.Server
{
    internal static class ComNetOS
    {
        // Minimum support for Windows 7 is assumed.
        internal static readonly bool IsWin8orLater;

        static ComNetOS()
        {
#if NET45
            var win8Version = new Version(6, 2);
            IsWin8orLater = (Environment.OSVersion.Version >= win8Version);
#else
            IsWin8orLater = true;
#endif
        }
    }
}
