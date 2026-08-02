// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// A marker interface which can be used to identify antiforgery metadata.
/// </summary>
public interface IAntiforgeryMetadata
{
    /// <summary>
    /// Gets a value indicating whether the antiforgery token should be validated.
    /// </summary>
    bool RequiresValidation { get; }
}
