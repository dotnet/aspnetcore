// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Contains options for the <see cref="DataProtectorTokenProvider{TUser}"/>.
/// </summary>
public class DataProtectionTokenProviderOptions
{
    /// <summary>
    /// Gets or sets the name of the <see cref="DataProtectorTokenProvider{TUser}"/>. Defaults to DataProtectorTokenProvider.
    /// </summary>
    /// <value>
    /// The name of the <see cref="DataProtectorTokenProvider{TUser}"/>.
    /// </value>
    public string Name { get; set; } = "DataProtectorTokenProvider";

    /// <summary>
    /// Gets or sets the amount of time a generated token remains valid. Defaults to 1 day.
    /// </summary>
    /// <value>
    /// The amount of time a generated token remains valid.
    /// </value>
    public TimeSpan TokenLifespan { get; set; } = TimeSpan.FromDays(1);
}
