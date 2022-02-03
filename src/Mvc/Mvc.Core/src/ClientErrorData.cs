// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Mvc;

/// <summary>
/// Information for producing client errors. This type is used to configure client errors
/// produced by consumers of <see cref="ApiBehaviorOptions.ClientErrorMapping"/>.
/// </summary>
public class ClientErrorData
{
    /// <summary>
    /// Gets or sets a link (URI) that describes the client error.
    /// </summary>
    /// <remarks>
    /// By default, this maps to <see cref="ProblemDetails.Type"/>.
    /// </remarks>
    public string? Link { get; set; }

    /// <summary>
    /// Gets or sets the summary of the client error.
    /// </summary>
    /// <remarks>
    /// By default, this maps to <see cref="ProblemDetails.Title"/> and should not change
    /// between multiple occurrences of the same error.
    /// </remarks>
    public string? Title { get; set; }
}
