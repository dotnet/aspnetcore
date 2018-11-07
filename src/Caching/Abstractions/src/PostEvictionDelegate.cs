// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Signature of the callback which gets called when a cache entry expires.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="reason">The <see cref="EvictionReason"/>.</param>
    /// <param name="state">The information that was passed when registering the callback.</param>
    public delegate void PostEvictionDelegate(object key, object value, EvictionReason reason, object state);
}