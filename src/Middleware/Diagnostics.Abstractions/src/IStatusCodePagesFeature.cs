// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Represents the Status code pages feature.
/// </summary>
public interface IStatusCodePagesFeature
{
    /// <summary>
    /// Indicates if the status code middleware will handle responses.
    /// </summary>
    bool Enabled { get; set; }
}
