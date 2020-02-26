// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Caching.Memory
{
    /// <summary>
    /// Signature of the callback which gets called when a cache entry expires.
    /// </summary>
    /// <param name="key">The key of the entry being evicted.</param>
    /// <param name="value">The value of the entry being evicted.</param>
    /// <param name="reason">The <see cref="EvictionReason"/>.</param>
    /// <param name="state">The information that was passed when registering the callback.</param>
    public delegate void PostEvictionDelegate(object key, object value, EvictionReason reason, object state);
}
