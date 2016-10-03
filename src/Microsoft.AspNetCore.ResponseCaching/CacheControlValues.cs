// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.ResponseCaching.Internal
{
    internal class CacheControlValues
    {
        public const string MaxAgeString = "max-age";
        public const string MaxStaleString = "max-stale";
        public const string MinFreshString = "min-fresh";
        public const string MustRevalidateString = "must-revalidate";
        public const string NoCacheString = "no-cache";
        public const string NoStoreString = "no-store";
        public const string NoTransformString = "no-transform";
        public const string OnlyIfCachedString = "only-if-cached";
        public const string PrivateString = "private";
        public const string ProxyRevalidateString = "proxy-revalidate";
        public const string PublicString = "public";
        public const string SharedMaxAgeString = "s-maxage";
    }
}
