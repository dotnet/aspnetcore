// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

/// <summary>
/// Interface used to store instances of <see cref="DataProtectionKey"/> in a <see cref="DbContext"/>
/// </summary>
public interface IDataProtectionKeyContext
{
    /// <summary>
    /// A collection of <see cref="DataProtectionKey"/>
    /// </summary>
    DbSet<DataProtectionKey> DataProtectionKeys { get; }
}
