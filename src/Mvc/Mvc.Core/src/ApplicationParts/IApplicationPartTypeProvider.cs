// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// Exposes a set of types from an <see cref="ApplicationPart"/>.
/// </summary>
public interface IApplicationPartTypeProvider
{
    /// <summary>
    /// Gets the list of available types in the <see cref="ApplicationPart"/>.
    /// </summary>
    IEnumerable<TypeInfo> Types { get; }
}
