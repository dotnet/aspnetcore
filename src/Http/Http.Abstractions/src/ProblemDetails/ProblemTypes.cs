// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Http;

/// <summary>
/// Represents the possible problem details types.
/// </summary>
[Flags]
public enum ProblemDetailsTypes : uint
{
    /// <summary>
    /// Specifies no types.
    /// </summary>
    None = 0,

    /// <summary>
    /// HTTP Status code 5xx
    /// </summary>
    Server = 1,

    /// <summary>
    /// Failures occurred during the routing system processing.
    /// </summary>
    Routing = 2,

    /// <summary>
    /// HTTP Status code 4xx
    /// </summary>
    Client = 4,

    /// <summary>
    /// Specifies all types.
    /// </summary>
    All = Routing | Server | Client,
}
