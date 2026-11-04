// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Abstracts the system clock to facilitate testing.
/// </summary>
[Obsolete("Use TimeProvider instead.")]
public interface ISystemClock
{
    /// <summary>
    /// Retrieves the current system time in UTC.
    /// </summary>
    DateTimeOffset UtcNow { get; }
}
