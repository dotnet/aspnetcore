// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Options for the WelcomePageMiddleware.
/// </summary>
public class WelcomePageOptions
{
    /// <summary>
    /// Specifies which requests paths will be responded to. Exact matches only. Leave null to handle all requests.
    /// </summary>
    public PathString Path { get; set; }
}
