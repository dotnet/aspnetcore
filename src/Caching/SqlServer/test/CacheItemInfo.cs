// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Caching.SqlServer
{
    public class CacheItemInfo
    {
        public string Id { get; set; }

        public byte[] Value { get; set; }

        public DateTimeOffset ExpiresAtTime { get; set; }

        public TimeSpan? SlidingExpirationInSeconds { get; set; }

        public DateTimeOffset? AbsoluteExpiration { get; set; }
    }
}
