// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.HttpSys;

/// <summary>
/// Enum declaring the allowed values for the verbosity level when http.sys reject requests due to throttling.
/// </summary>
public enum Http503VerbosityLevel : long
{
    /// <summary>
    /// A 503 response is not sent; the connection is reset. This is the default HTTP Server API behavior.
    /// </summary>
    Basic = 0,

    /// <summary>
    /// The HTTP Server API sends a 503 response with a "Service Unavailable" reason phrase.
    /// </summary>
    Limited = 1,

    /// <summary>
    /// The HTTP Server API sends a 503 response with a detailed reason phrase.
    /// </summary>
    Full = 2
}
