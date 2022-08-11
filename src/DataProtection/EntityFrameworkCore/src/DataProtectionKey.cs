// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

/// <summary>
/// Code first model used by <see cref="EntityFrameworkCoreXmlRepository{TContext}"/>.
/// </summary>
// DataProtectionKey.Id is not used anywhere. Add DynamicallyAccessedMembers to prevent it from being trimmed.
// Note that in the future EF may annotate itself to include properties automatically, and the annotation here could be removed.
// Fixes https://github.com/dotnet/aspnetcore/issues/43187
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
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
