// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc.ApplicationParts;

/// <summary>
/// Exposes one or more reference paths from an <see cref="ApplicationPart"/>.
/// </summary>
public interface ICompilationReferencesProvider
{
    /// <summary>
    /// Gets reference paths used to perform runtime compilation.
    /// </summary>
    IEnumerable<string> GetReferencePaths();
}
