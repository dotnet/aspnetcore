// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal
{
    public interface ICacheableKeyRingProvider
    {
        CacheableKeyRing GetCacheableKeyRing(DateTimeOffset now);
    }
}
