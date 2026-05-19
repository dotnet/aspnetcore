// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Diagnostics;

/// <summary>
/// Represents the Status code pages feature.
/// </summary>
public class StatusCodePagesFeature : IStatusCodePagesFeature
{
    /// <summary>
    /// Enables or disables status code pages. The default value is true.
    /// Set this to false to prevent the <see cref="StatusCodePagesMiddleware"/>
    /// from creating a response body while handling the error status code.
    /// </summary>
    public bool Enabled { get; set; } = true;
}
