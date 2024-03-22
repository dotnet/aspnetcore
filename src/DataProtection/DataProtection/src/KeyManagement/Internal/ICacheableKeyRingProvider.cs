// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;

/// <summary>
/// This API supports infrastructure and is not intended to be used
/// directly from your code. This API may change or be removed in future releases.
/// </summary>
/// <remarks>
/// This interface is retained for the benefit of consumers who were casting <see cref="IKeyRingProvider"/>
/// (also pseudo-internal) to <see cref="ICacheableKeyRingProvider"/> so that they could invoke
/// <see cref="GetCacheableKeyRing"/>.  That method returns an object with no public properties and doesn't
/// update the state of the <see cref="IKeyRingProvider"/>, but it does trigger calls to the backing
/// <see cref="IKeyManager"/>, which may have observable effects.  It is no longer used as a test hook.
/// </remarks>
public interface ICacheableKeyRingProvider
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    CacheableKeyRing GetCacheableKeyRing(DateTimeOffset now);
}
