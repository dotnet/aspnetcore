// Copyright (c) .NET Foundation. All rights reserved. 
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System.Collections.Concurrent;
using System.Globalization;

namespace Microsoft.Framework.Globalization
{
    /// <summary>
    /// Provides read-only cached instances of <see cref="CultureInfo"/>.
    /// </summary>
    public static partial class CultureInfoCache
    {
        private static readonly ConcurrentDictionary<string, CacheEntry> _cache = new ConcurrentDictionary<string, CacheEntry>();

        /// <summary>
        /// Gets a read-only cached <see cref="CultureInfo"/> for the specified name. Only names that exist in
        /// <see cref="KnownCultureNames"/> will be used.
        /// </summary>
        /// <param name="name">The culture name.</param>
        /// <returns>
        /// A read-only cached <see cref="CultureInfo"/> or <c>null</c> a match wasn't found in
        /// <see cref="KnownCultureNames"/>.
        /// </returns>
        public static CultureInfo GetCultureInfo(string name)
        {
            // Allow only known culture names as this API is called with input from users (HTTP requests) and
            // creating CultureInfo objects is expensive and we don't want it to throw either.
            if (name == null || !KnownCultureNames.Contains(name))
            {
                return null;
            }

            var entry = _cache.GetOrAdd(name, n =>
            {
                try
                {
                    return new CacheEntry(CultureInfo.ReadOnly(new CultureInfo(n)));
                }
                catch (CultureNotFoundException)
                {
                    // This can still throw as the list of culture names we have is generated from latest .NET Framework
                    // on latest Windows and thus contains names that won't be supported on lower framework or OS versions.
                    // We can just cache the null result in these cases as it's ultimately bound by the list anyway.
                    return new CacheEntry(cultureInfo: null);
                }
            });

            return entry.CultureInfo;
        }

        private class CacheEntry
        {
            public CacheEntry(CultureInfo cultureInfo)
            {
                CultureInfo = cultureInfo;
            }

            public CultureInfo CultureInfo { get; }
        }
    }
}
