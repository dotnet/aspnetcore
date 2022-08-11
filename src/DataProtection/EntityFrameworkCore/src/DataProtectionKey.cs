// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

/// <summary>
/// Code first model used by <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
/// </summary>
public class DataProtectionKey
{
    /// <summary>
    /// The entity identifier of the <see cref="DataProtectionKey"/>.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The friendly name of the <see cref="DataProtectionKey"/>.
    /// </summary>
    public string? FriendlyName { get; set; }

    /// <summary>
    /// The XML representation of the <see cref="DataProtectionKey"/>.
    /// </summary>
    public string? Xml { get; set; }
}
