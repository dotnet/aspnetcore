// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.AI;

namespace ComponentsAIClaimApp.Data;

/// <summary>
/// Represents a vehicle photo selected for the current claim.
/// </summary>
public sealed record ClaimPhotoPreview(DataContent Content)
{
    /// <summary>
    /// Gets the display name of the photo.
    /// </summary>
    public string Name => Content.Name ?? "Vehicle photo";
}
