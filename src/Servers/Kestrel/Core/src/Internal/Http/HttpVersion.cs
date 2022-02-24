// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

/// <summary>
/// This API supports framework infrastructure and is not intended to be used
/// directly from application code.
/// </summary>
public enum HttpVersion : sbyte
{
    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Http10 = 0,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Http11 = 1,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Http2 = 2,

    /// <summary>
    /// This API supports framework infrastructure and is not intended to be used
    /// directly from application code.
    /// </summary>
    Http3 = 3
}
