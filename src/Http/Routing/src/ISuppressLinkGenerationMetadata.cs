// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Represents metadata used during link generation. If <see cref="SuppressLinkGeneration"/> is <c>true</c>
/// the associated endpoint will not be used for link generation.
/// </summary>
public interface ISuppressLinkGenerationMetadata
{
    /// <summary>
    /// Gets a value indicating whether the associated endpoint should be used for link generation.
    /// </summary>
    bool SuppressLinkGeneration { get; }
}
