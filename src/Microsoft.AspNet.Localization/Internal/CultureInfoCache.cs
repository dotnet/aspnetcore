// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace Microsoft.AspNet.Localization.Internal
{
    public static class CultureInfoCache
    {
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();

        public static CultureInfo GetCultureInfo(string name, bool throwIfNotFound = false)
        {
            // Allow empty string values as they map to InvariantCulture, whereas null culture values will throw in
            // the CultureInfo ctor
            if (name == null)
            {
                return null;
            }

            var entry = _cache.GetOrAdd(name, n =>
            {
                try
                {
                    return new CacheEntry(CultureInfo.ReadOnly(new CultureInfo(n)));
                }
                catch (CultureNotFoundException ex)
                {
                    return new CacheEntry(ex);
                }
            });

            if (entry.Exception != null && throwIfNotFound)
            {
                throw entry.Exception;
            }

            return entry.CultureInfo;
        }

        private class CacheEntry
        {
            public CacheEntry(CultureInfo cultureInfo)
            {
                CultureInfo = cultureInfo;
            }

            public CacheEntry(Exception exception)
            {
                Exception = exception;
            }

            public CultureInfo CultureInfo { get; }

            public Exception Exception { get; }
        }
    }
}
