// -----------------------------------------------------------------------
// <copyright file="NclUtilities.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;

namespace Microsoft.AspNet.Server.WebListener
{
    internal static class NclUtilities
    {
        internal static bool HasShutdownStarted
        {
            get
            {
                return Environment.HasShutdownStarted
#if NET45
                    || AppDomain.CurrentDomain.IsFinalizingForUnload()
#endif
                    ;
            }
        }
    }
}
