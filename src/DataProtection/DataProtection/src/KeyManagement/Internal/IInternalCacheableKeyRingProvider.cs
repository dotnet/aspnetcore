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
/// Replaces <see cref="ICacheableKeyRingProvider"/> as a test hook for validating
/// <see cref="KeyRingProvider"/>.
/// </remarks>
internal interface IInternalCacheableKeyRingProvider
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    CacheableKeyRing GetCacheableKeyRing(DateTimeOffset now, bool allowShortRefreshPeriod = true);
}
