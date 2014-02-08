// -----------------------------------------------------------------------
// <copyright file="SSPIHandleCache.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

/*
Abstract:
    The file implements trivial SSPI credential caching mechanism based on lru list
*/
using System;
using System.Threading;

namespace Microsoft.AspNet.Security.Windows
{
    // Implements delayed SSPI handle release, like a finalizable object though the handles are kept alive until being pushed out
    // by the newly incoming ones.
    internal static class SSPIHandleCache
    {
        private const int MaxCacheSize = 0x1F;  // must a (power of 2) - 1
        private static SafeCredentialReference[] _cacheSlots = new SafeCredentialReference[MaxCacheSize + 1];
        private static int _current = -1;

        internal static void CacheCredential(SafeFreeCredentials newHandle)
        {
            try
            {
                SafeCredentialReference newRef = SafeCredentialReference.CreateReference(newHandle);
                if (newRef == null)
                {
                    return;
                }
                unchecked
                {
                    int index = Interlocked.Increment(ref _current) & MaxCacheSize;
                    newRef = Interlocked.Exchange<SafeCredentialReference>(ref _cacheSlots[index], newRef);
                }
                if (newRef != null)
                {
                    newRef.Dispose();
                }
            }
            catch (Exception e)
            {
                GlobalLog.Assert("SSPIHandlCache", "Attempted to throw: " + e.ToString());
            }
        }
    }
}
