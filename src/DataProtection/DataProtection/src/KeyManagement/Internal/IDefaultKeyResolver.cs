// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.DataProtection.KeyManagement.Internal;

/// <summary>
/// Implements policy for resolving the default key from a candidate keyring.
/// </summary>
public interface IDefaultKeyResolver
{
    /// <summary>
    /// Locates the default key from the keyring.
    /// </summary>
    DefaultKeyResolution ResolveDefaultKeyPolicy(DateTimeOffset now, IEnumerable<IKey> allKeys);
}
