// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Metadata used to prevent URL matching. If <see cref="SuppressMatching"/> is <c>true</c> the
/// associated endpoint will not be considered for URL matching.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class SuppressMatchingMetadata : ISuppressMatchingMetadata
{
    /// <summary>
    /// Gets a value indicating whether the associated endpoint should be used for URL matching.
    /// </summary>
    public bool SuppressMatching => true;

    /// <inheritdoc/>
    public override string ToString()
    {
        return DebuggerHelpers.GetDebugText(nameof(SuppressMatching), SuppressMatching);
    }
}
