// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntryStack
    {
        private readonly CacheEntryStack _previous;
        private readonly CacheEntry _entry;

        private CacheEntryStack()
        {
        }

        private CacheEntryStack(CacheEntryStack previous, CacheEntry entry)
        {
            if (previous == null)
            {
                throw new ArgumentNullException(nameof(previous));
            }

            _previous = previous;
            _entry = entry;
        }

        public static CacheEntryStack Empty { get; } = new CacheEntryStack();

        public CacheEntryStack Push(CacheEntry c)
        {
            return new CacheEntryStack(this, c);
        }

        public CacheEntry Peek()
        {
            return _entry;
        }
    }
}
