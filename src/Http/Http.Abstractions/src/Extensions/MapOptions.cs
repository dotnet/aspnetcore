// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder.Extensions;

/// <summary>
/// Options for the <see cref="MapMiddleware"/>.
/// </summary>
public class MapOptions
{
    /// <summary>
    /// The path to match.
    /// </summary>
    public PathString PathMatch { get; set; }

    /// <summary>
    /// The branch taken for a positive match.
    /// </summary>
    public RequestDelegate? Branch { get; set; }

    /// <summary>
    /// If false, matched path would be removed from Request.Path and added to Request.PathBase
    /// Defaults to false.
    /// </summary>
    public bool PreserveMatchedPathSegment { get; set; }
}
